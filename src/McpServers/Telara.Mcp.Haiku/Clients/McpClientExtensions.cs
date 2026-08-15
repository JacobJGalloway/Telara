using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Telara.Mcp.Haiku.Clients;

public static class McpClientExtensions
{
    // Shared by every Haiku tool that passes a call through to the Internal Functionality
    // Server: extracts the text content and throws McpException on IsError so the failure
    // (with its real message - see EquipmentOperationsTools.ClaimEquipmentLease in
    // Telara.Mcp.Internal) keeps propagating rather than being swallowed at this hop.
    public static async Task<string> CallAndUnwrapAsync(
        this McpClient client, string toolName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var result = await client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;

        if (result.IsError == true)
            throw new McpException(text ?? $"{toolName} failed on the Internal Functionality Server.");

        return text ?? string.Empty;
    }
}
