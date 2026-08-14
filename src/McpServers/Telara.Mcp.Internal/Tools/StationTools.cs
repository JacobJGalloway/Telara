using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Server;
using Telara.Domain.CQRS.Commands;

namespace Telara.Mcp.Internal.Tools;

[McpServerToolType]
public static class StationTools
{
    [McpServerTool(Name = "register_station")]
    [Description("Registers a new Station at a facility. Fails if a Station with the same FacilityId/StationId already exists.")]
    public static async Task<RegisterStationResult> RegisterStation(
        ISender sender,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        CancellationToken cancellationToken) =>
        await sender.Send(new RegisterStationCommand(facilityId, stationId), cancellationToken);
}
