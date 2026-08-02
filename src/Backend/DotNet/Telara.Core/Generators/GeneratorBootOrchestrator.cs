using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telara.Core.Generators.Interfaces;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Exceptions;
using Telara.Domain.Telemetry;

namespace Telara.Core.Generators;

public class GeneratorBootOrchestrator(
    IOptions<GeneratorSettings> settings,
    IGeneratorInstanceFactory factory,
    GeneratorRegistry registry,
    ISensorValueGenerator valueGenerator,
    IServiceScopeFactory scopeFactory,
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
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            await sender.Send(
                new ClaimStationEquipmentLeaseCommand(
                    instance.Config.FacilityId,
                    instance.Config.StationId,
                    instance.Config.EquipmentId,
                    instance.Config.EquipmentTypeId,
                    instance.Config.InstanceId,
                    instance.Config.LeaseDuration),
                cancellationToken);

            instance.LastLeaseClaimUtc = DateTime.UtcNow;
            return true;
        }
        catch (StationEquipmentAlreadyOperationalException ex)
        {
            // Expected business outcome, not a system error - see ARCHITECTURE.md Get-or-Create/Lease Semantics.
            logger.LogInformation(
                "Generator instance {InstanceId} could not claim/renew the lease for equipment {EquipmentId} - already held by another active instance.",
                instance.Config.InstanceId, ex.ConflictingKey.TelemetryId);
            return false;
        }
    }

    private async Task WriteReadings(GeneratorInstance instance, CancellationToken cancellationToken)
    {
        if (instance.Config.Sensors.Count == 0)
            return;

        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var readings = instance.Config.Sensors
            .Select(sensor => new SensorReadingInput(sensor.SensorId, sensor.ReadingType, valueGenerator.NextValue(sensor)))
            .ToList();

        await sender.Send(
            new RecordSensorReadingsCommand(instance.Config.FacilityId, instance.Config.StationId, instance.Config.EquipmentId, readings),
            cancellationToken);
    }
}
