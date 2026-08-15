using System.ComponentModel;
using Mediator;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Exceptions;

namespace Telara.Mcp.Internal.Tools;

[McpServerToolType]
public static class EquipmentOperationsTools
{
    [McpServerTool(Name = "claim_equipment_lease")]
    [Description("Claims or renews a generator instance's write lease on a piece of StationEquipment. Creates the row on first claim. Fails if the lease is already held by a different instance.")]
    public static async Task<ClaimStationEquipmentLeaseResult> ClaimEquipmentLease(
        ISender sender,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Equipment identifier")] string equipmentId,
        [Description("EquipmentTypes reference table id")] int equipmentTypeId,
        [Description("Stable id of the generator instance claiming the lease")] string instanceId,
        [Description("Lease duration, e.g. '00:15:00'")] TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(
                new ClaimStationEquipmentLeaseCommand(facilityId, stationId, equipmentId, equipmentTypeId, instanceId, leaseDuration),
                cancellationToken);
        }
        catch (StationEquipmentAlreadyOperationalException ex)
        {
            // Only McpException's Message survives the MCP boundary verbatim - any other
            // exception type is flattened to a generic message by the SDK to avoid leaking
            // internals. This is an expected business outcome (see ARCHITECTURE.md's
            // Get-or-Create/Lease Semantics), and callers (Haiku, then the generator via MAF)
            // need the real reason to distinguish "back off" from "something is broken."
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "record_sensor_readings")]
    [Description("Persists a batch of sensor readings for a piece of StationEquipment. Returns false if the equipment isn't registered.")]
    public static async Task<bool> RecordSensorReadings(
        ISender sender,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Equipment identifier")] string equipmentId,
        [Description("Readings to persist")] IReadOnlyList<SensorReadingInput> readings,
        CancellationToken cancellationToken) =>
        await sender.Send(new RecordSensorReadingsCommand(facilityId, stationId, equipmentId, readings), cancellationToken);
}
