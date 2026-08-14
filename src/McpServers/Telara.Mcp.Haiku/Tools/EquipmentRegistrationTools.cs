using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
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

        var result = await client.CallToolAsync(
            "register_station_equipment",
            new Dictionary<string, object>
            {
                ["facilityId"] = facilityId,
                ["stationId"] = stationId,
                ["equipmentId"] = equipmentId,
                ["equipmentTypeId"] = equipmentTypeId,
            },
            cancellationToken: cancellationToken);

        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;

        if (result.IsError == true)
            throw new McpException(text ?? "register_station_equipment failed on the Internal Functionality Server.");

        return text ?? string.Empty;
    }
}
