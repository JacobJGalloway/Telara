using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telara.Core.Generators.Interfaces;
using Telara.Core.Maf;
using Telara.Domain.Telemetry;

namespace Telara.Core.Generators;

public class GeneratorBootOrchestrator(
    IOptions<GeneratorSettings> settings,
    IGeneratorInstanceFactory factory,
    GeneratorRegistry registry,
    ISensorValueGenerator valueGenerator,
    MafClient mafClient,
    ILogger<GeneratorBootOrchestrator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var instances = BuildAndAudit();
        var loops = new List<Task>();

        foreach (var instance in instances)
        {
            registry.TryAdd(instance);

            if (await ClaimLease(instance, stoppingToken))
                loops.Add(RunGenerationLoop(instance, stoppingToken));
        }

        await Task.WhenAll(loops);
    }

    private List<GeneratorInstance> BuildAndAudit()
    {
        var seen = new HashSet<TelemetryKey>();
        var instances = new List<GeneratorInstance>();

        foreach (var config in settings.Value.Instances)
        {
            var instance = factory.Create(config);

            if (!seen.Add(instance.Key))
                throw new DuplicateTelemetryKeyException(instance.Key);

            instances.Add(instance);
        }

        return instances;
    }

    private async Task RunGenerationLoop(GeneratorInstance instance, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(instance.Config.GenerationInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // Renewing the lease and writing readings on the same tick means one signal
                // covers both "I produced a reading" and "I'm still alive" - same shape as a
                // token refresh extending validity on each use rather than on a separate timer.
                if (!await ClaimLease(instance, stoppingToken))
                    break;

                await WriteReadings(instance, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task<bool> ClaimLease(GeneratorInstance instance, CancellationToken cancellationToken)
    {
        var arguments = new Dictionary<string, object>
        {
            ["facilityId"] = instance.Config.FacilityId,
            ["stationId"] = instance.Config.StationId,
            ["equipmentId"] = instance.Config.EquipmentId,
            ["equipmentTypeId"] = instance.Config.EquipmentTypeId,
            ["instanceId"] = instance.Config.InstanceId,
            ["leaseDuration"] = instance.Config.LeaseDuration.ToString(),
        };

        var result = await mafClient.CallOperationalAsync("claim_equipment_lease", arguments, cancellationToken);

        if (!result.IsError)
        {
            instance.LastLeaseClaimUtc = DateTime.UtcNow;
            return true;
        }

        // MAF/the MCP SDK don't carry structured error codes this sprint (see "Failure
        // handling" in ARCHITECTURE.md's MAF Orchestrator section) - this is a message-content
        // check, not a typed one, so it's a known limitation, not an oversight. Anything that
        // doesn't match the expected lease-conflict text is treated as a genuine failure and
        // left to propagate (BackgroundServiceExceptionBehavior.StopHost), rather than silently
        // handled the same way as the expected case.
        if (result.FirstText?.Contains("already leased by another active instance") == true)
        {
            // Expected business outcome, not a system error - see ARCHITECTURE.md Get-or-Create/Lease Semantics.
            logger.LogInformation(
                "Generator instance {InstanceId} could not claim/renew the lease for equipment {EquipmentId} - already held by another active instance.",
                instance.Config.InstanceId, instance.Config.EquipmentId);
            return false;
        }

        throw new InvalidOperationException($"claim_equipment_lease failed for {instance.Config.EquipmentId}: {result.FirstText}");
    }

    private async Task WriteReadings(GeneratorInstance instance, CancellationToken cancellationToken)
    {
        if (instance.Config.Sensors.Count == 0)
            return;

        var readings = instance.Config.Sensors
            .Select(sensor => new Dictionary<string, object>
            {
                ["sensorId"] = sensor.SensorId,
                ["readingType"] = sensor.ReadingType,
                ["value"] = valueGenerator.NextValue(sensor),
            })
            .ToList();

        var arguments = new Dictionary<string, object>
        {
            ["facilityId"] = instance.Config.FacilityId,
            ["stationId"] = instance.Config.StationId,
            ["equipmentId"] = instance.Config.EquipmentId,
            ["readings"] = readings,
        };

        var result = await mafClient.CallOperationalAsync("ingest_sensor_readings", arguments, cancellationToken);

        if (result.IsError)
            throw new InvalidOperationException($"ingest_sensor_readings failed for {instance.Config.EquipmentId}: {result.FirstText}");
    }
}
