using System.ComponentModel;
using ModelContextProtocol.Server;
using Telara.Mcp.Haiku.Clients;

namespace Telara.Mcp.Haiku.Tools;

public record EquipmentReadingInput(
    [property: Description("Sensor identifier")] string SensorId,
    [property: Description("Reading type, e.g. BeltSpeed")] string ReadingType,
    [property: Description("Reading value")] decimal Value);

[McpServerToolType]
public static class EquipmentOperationsTools
{
    [McpServerTool(Name = "claim_equipment_lease")]
    [Description("Drives the generator's lease-claim workflow: hands the claim off to the Internal Functionality Server, which owns lease semantics and persistence.")]
    public static async Task<string> ClaimEquipmentLease(
        InternalMcpClientProvider internalClientProvider,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Equipment identifier")] string equipmentId,
        [Description("EquipmentTypes reference table id")] int equipmentTypeId,
        [Description("Stable id of the generator instance claiming the lease")] string instanceId,
        [Description("Lease duration, e.g. '00:15:00'")] TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var client = await internalClientProvider.GetClientAsync(cancellationToken);

        return await client.CallAndUnwrapAsync(
            "claim_equipment_lease",
            new Dictionary<string, object>
            {
                ["facilityId"] = facilityId,
                ["stationId"] = stationId,
                ["equipmentId"] = equipmentId,
                ["equipmentTypeId"] = equipmentTypeId,
                ["instanceId"] = instanceId,
                ["leaseDuration"] = leaseDuration.ToString(),
            },
            cancellationToken);
    }

    [McpServerTool(Name = "ingest_sensor_readings")]
    [Description("Drives the sensor-ingestion workflow: runs raw generator readings through the ETL adapter shape and hands them to the Internal Functionality Server for persistence. Haiku's own tools never touch the domain model directly.")]
    public static async Task<string> IngestSensorReadings(
        InternalMcpClientProvider internalClientProvider,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Equipment identifier")] string equipmentId,
        [Description("Readings to persist")] IReadOnlyList<EquipmentReadingInput> readings,
        CancellationToken cancellationToken)
    {
        var client = await internalClientProvider.GetClientAsync(cancellationToken);

        return await client.CallAndUnwrapAsync(
            "record_sensor_readings",
            new Dictionary<string, object>
            {
                ["facilityId"] = facilityId,
                ["stationId"] = stationId,
                ["equipmentId"] = equipmentId,
                ["readings"] = readings.Select(r => new Dictionary<string, object>
                {
                    ["sensorId"] = r.SensorId,
                    ["readingType"] = r.ReadingType,
                    ["value"] = r.Value,
                }).ToList(),
            },
            cancellationToken);
    }

    [McpServerTool(Name = "ingest_station_output")]
    [Description("Drives the station-output workflow: hands a cumulative units-produced snapshot off to the Internal Functionality Server for persistence. Haiku's own tools never touch the domain model directly.")]
    public static async Task<string> IngestStationOutput(
        InternalMcpClientProvider internalClientProvider,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Cumulative units produced so far this shift")] decimal unitsProduced,
        CancellationToken cancellationToken)
    {
        var client = await internalClientProvider.GetClientAsync(cancellationToken);

        return await client.CallAndUnwrapAsync(
            "record_station_output",
            new Dictionary<string, object>
            {
                ["facilityId"] = facilityId,
                ["stationId"] = stationId,
                ["unitsProduced"] = unitsProduced,
            },
            cancellationToken);
    }
}
