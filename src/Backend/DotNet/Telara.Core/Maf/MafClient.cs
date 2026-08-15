using System.Net.Http.Json;
using System.Text.Json;

namespace Telara.Core.Maf;

public record MafRouteResult(bool IsError, IReadOnlyList<string> Content)
{
    public string? FirstText => Content.Count > 0 ? Content[0] : null;
}

public class MafClientOptions
{
    public const string SectionName = "Maf";

    public string BaseUrl { get; set; } = default!;
}

// The generator's and OpsApi's one and only route to the MCP servers, per "MAF Orchestrator
// (This Sprint)" in ARCHITECTURE.md - callers here are internal .NET code, not an LLM, so this
// is a plain typed HttpClient against MAF's routing API, not an MCP client itself.
public class MafClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<MafRouteResult> CallOperationalAsync(string toolName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken) =>
        CallAsync("/route/operational", toolName, arguments, cancellationToken);

    public Task<MafRouteResult> CallReadAsync(string toolName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken) =>
        CallAsync("/route/read", toolName, arguments, cancellationToken);

    private async Task<MafRouteResult> CallAsync(
        string path, string toolName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(path, new { toolName, arguments }, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MafRouteResult>(JsonOptions, cancellationToken);
        return result ?? new MafRouteResult(true, [$"MAF returned an empty response for {toolName}."]);
    }
}
