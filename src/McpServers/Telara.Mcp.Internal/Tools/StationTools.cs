using System.ComponentModel;
using Mediator;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Exceptions;

namespace Telara.Mcp.Internal.Tools;

[McpServerToolType]
public static class StationTools
{
    [McpServerTool(Name = "register_station")]
    [Description("Registers a new Station at a facility, optionally linking one or more existing predecessor Stations to feed into it. Fails if a Station with the same FacilityId/StationId already exists, a predecessor doesn't exist, or a predecessor already has a successor/is a loading dock.")]
    public static async Task<RegisterStationResult> RegisterStation(
        ISender sender,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Existing Station ids (same facility) whose NextStationId should point at this new Station - a funnel of multiple predecessors into one successor is allowed, an empty list means this Station starts a new line")] IReadOnlyList<string>? predecessorStationIds,
        [Description("Whether this Station is a line's terminal/output node")] bool isLoadingDock,
        CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(new RegisterStationCommand(facilityId, stationId, predecessorStationIds ?? [], isLoadingDock), cancellationToken);
        }
        catch (Exception ex) when (ex is StationAlreadyExistsException or StationNotFoundException or PredecessorLinkConflictException)
        {
            // See EquipmentOperationsTools.ClaimEquipmentLease - only McpException's Message
            // survives the MCP boundary verbatim, so domain exceptions must be rethrown as one.
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "register_station_equipment")]
    [Description("Registers a new StationEquipment record under an existing Station. Fails if the Station doesn't exist or the equipment is already registered.")]
    public static async Task<RegisterStationEquipmentResult> RegisterStationEquipment(
        ISender sender,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Equipment identifier")] string equipmentId,
        [Description("EquipmentTypes reference table id")] int equipmentTypeId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(new RegisterStationEquipmentCommand(facilityId, stationId, equipmentId, equipmentTypeId), cancellationToken);
        }
        catch (Exception ex) when (ex is StationNotFoundException or StationEquipmentAlreadyRegisteredException)
        {
            throw new McpException(ex.Message);
        }
    }
}
