using System.ComponentModel;
using ModelContextProtocol.Server;
using Telara.Mcp.Haiku.Clients;

namespace Telara.Mcp.Haiku.Tools;

[McpServerToolType]
public static class EquipmentRegistrationTools
{
    [McpServerTool(Name = "register_equipment")]
    [Description("Drives the equipment-registration workflow: hands raw registration data to the Internal Functionality Server, which formats it into a domain model and persists it. Haiku's own tools never touch the domain model directly.")]
    public static async Task<string> RegisterEquipment(
        InternalMcpClientProvider internalClientProvider,
        [Description("Facility identifier")] string facilityId,
        [Description("Station identifier")] string stationId,
        [Description("Equipment identifier")] string equipmentId,
        [Description("EquipmentTypes reference table id")] int equipmentTypeId,
        CancellationToken cancellationToken)
    {
        var client = await internalClientProvider.GetClientAsync(cancellationToken);

        return await client.CallAndUnwrapAsync(
            "register_station_equipment",
            new Dictionary<string, object>
            {
                ["facilityId"] = facilityId,
                ["stationId"] = stationId,
                ["equipmentId"] = equipmentId,
                ["equipmentTypeId"] = equipmentTypeId,
            },
            cancellationToken);
    }
}
