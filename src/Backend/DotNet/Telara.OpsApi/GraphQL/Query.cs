using System.Text.Json;
using Mediator;
using HotChocolate.Authorization;
using Telara.Core.Maf;
using Telara.Domain.CQRS.Queries;
using Telara.Domain.Entities;
using Telara.OpsApi.GraphQL.Types;

namespace Telara.OpsApi.GraphQL;

[QueryType]
public static partial class Query
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string GetApiStatus() => "online";

    [Authorize]
    [UseFiltering]
    [UseSorting]
    public static async Task<IQueryable<SensorReading>> GetSensorReadings([Service] ISender mediator) =>
        await mediator.Send(new GetSensorReadingsQuery());

    // The "manual on-screen request" read path from ARCHITECTURE.md's Definition of Done -
    // routed through MAF to the Internal Functionality Server's get_latest_sensor_readings
    // tool, rather than calling Telara.Domain/MediatR directly like GetSensorReadings above.
    [Authorize]
    public static async Task<IReadOnlyList<LatestSensorReading>> GetLatestSensorReadings(
        [Service] MafClient mafClient,
        string facilityId,
        string stationId,
        string equipmentId,
        CancellationToken cancellationToken)
    {
        var arguments = new Dictionary<string, object>
        {
            ["facilityId"] = facilityId,
            ["stationId"] = stationId,
            ["equipmentId"] = equipmentId,
        };

        var result = await mafClient.CallReadAsync("get_latest_sensor_readings", arguments, cancellationToken);

        if (result.IsError)
            throw new GraphQLException($"get_latest_sensor_readings failed: {result.FirstText}");

        if (string.IsNullOrEmpty(result.FirstText))
            return [];

        return JsonSerializer.Deserialize<List<LatestSensorReading>>(result.FirstText, JsonOptions) ?? [];
    }

    // Bulk UI-shaping read for the workflow diagram screen - direct MediatR like GetSensorReadings
    // above, not MAF, per ARCHITECTURE.md's 1.3 "Decisions Made This Sprint".
    [Authorize]
    public static async Task<IReadOnlyList<WorkflowStation>> GetStationWorkflow(
        [Service] ISender mediator,
        string facilityId,
        string stationId,
        CancellationToken cancellationToken)
    {
        var stations = await mediator.Send(new GetStationWorkflowQuery(facilityId, stationId), cancellationToken);
        var readings = await mediator.Send(new GetSensorReadingsQuery(), cancellationToken);

        var latestByEquipment = readings
            .Where(r => r.FacilityId == facilityId)
            .AsEnumerable()
            .GroupBy(r => r.EquipmentId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(r => r.SensorId)
                    .Select(sg => sg.OrderByDescending(r => r.ReadingAtUtc).First())
                    .Select(r => new LatestSensorReading(r.SensorId, r.ReadingType, r.Value, r.ReadingAtUtc))
                    .ToList() as IReadOnlyList<LatestSensorReading>);

        return stations
            .Select(s => new WorkflowStation(
                s.StationId,
                s.IsLoadingDock,
                s.Equipment
                    .Select(e => new WorkflowEquipment(
                        e.EquipmentId,
                        e.EquipmentType?.Name ?? string.Empty,
                        e.Status.ToString(),
                        latestByEquipment.GetValueOrDefault(e.EquipmentId, [])))
                    .ToList()))
            .ToList();
    }
}
