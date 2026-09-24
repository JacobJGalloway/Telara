using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Server;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.CQRS.Queries;

namespace Telara.Mcp.Internal.Tools;

[McpServerToolType]
public static class SensorReadingTools
{
    [McpServerTool(Name = "record_station_output")]
    [Description("Persists a cumulative units-produced snapshot for a Station's current shift. Returns false if the Station isn't registered.")]
    public static async Task<bool> RecordStationOutput(
        ISender sender,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Cumulative units produced so far this shift")] decimal unitsProduced,
        CancellationToken cancellationToken) =>
        await sender.Send(new RecordStationOutputCommand(facilityId, stationId, unitsProduced), cancellationToken);

    [McpServerTool(Name = "get_latest_sensor_readings")]
    [Description("Returns the latest reading for each sensor on a given piece of station equipment.")]
    public static async Task<IReadOnlyList<LatestSensorReading>> GetLatestSensorReadings(
        ISender sender,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Equipment identifier")] string equipmentId,
        CancellationToken cancellationToken)
    {
        var readings = await sender.Send(new GetSensorReadingsQuery(), cancellationToken);

        return readings
            .Where(r => r.FacilityId == facilityId && r.StationId == stationId && r.EquipmentId == equipmentId)
            .AsEnumerable()
            .GroupBy(r => r.SensorId)
            .Select(g => g.OrderByDescending(r => r.ReadingAtUtc).First())
            .Select(r => new LatestSensorReading(r.SensorId, r.ReadingType, r.Value, r.ReadingAtUtc))
            .ToList();
    }
}

public record LatestSensorReading(string SensorId, string ReadingType, decimal Value, DateTime ReadingAtUtc);
