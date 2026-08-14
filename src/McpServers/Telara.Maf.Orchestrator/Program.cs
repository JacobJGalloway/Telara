using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Telara.Maf.Orchestrator.Clients;

var builder = WebApplication.CreateBuilder(args);

// MAF orchestrator, stateless router only this sprint - see "MAF Orchestrator (This Sprint)"
// in ARCHITECTURE.md. Sits in front of the two MCP servers and is the layer the GraphQL
// resolvers and the generator's write path call through, instead of either calling MCP
// tools directly. Not itself an MCP server: its callers are internal .NET code, not an LLM
// agent, so it exposes a plain routing API and acts as an MCP *client* to the two servers.
builder.Services.Configure<MafOptions>(builder.Configuration.GetSection(MafOptions.SectionName));
builder.Services.AddSingleton<McpServerClientRegistry>();
builder.Services.AddSingleton<McpToolCatalog>();

var app = builder.Build();

// Tool discovery: lazy, not eager. The first call to /tools connects to both MCP servers and
// caches what each one exposes, tagged by which server owns it; every call after that is
// served from the cache instead of re-querying tools/list. Pass ?refresh=true to force a
// re-query (e.g. after redeploying one of the servers with new/changed tools).
app.MapGet("/tools", async (McpToolCatalog catalog, bool? refresh, CancellationToken cancellationToken) =>
    Results.Ok(await catalog.GetToolsAsync(refresh ?? false, cancellationToken)));

// Routing rule from ARCHITECTURE.md's "MAF Orchestrator (This Sprint)": read tools (sensor/
// Station/StationEquipment lookups) go to the Internal Functionality Server; operational/write
// tools (registration, lease claims, sensor writes) go to the Claude Haiku Server. That's the
// entire decision MAF makes here - no retries, no chaining, no state. Each endpoint dispatches
// a single named tool call to its fixed target server and surfaces the result as-is.
app.MapPost("/route/read", async (RouteRequest request, McpServerClientRegistry clients, McpToolCatalog catalog, CancellationToken cancellationToken) =>
    await RouteToAsync("internal", await clients.GetInternalClientAsync(cancellationToken), catalog, request, cancellationToken));

app.MapPost("/route/operational", async (RouteRequest request, McpServerClientRegistry clients, McpToolCatalog catalog, CancellationToken cancellationToken) =>
    await RouteToAsync("haiku", await clients.GetHaikuClientAsync(cancellationToken), catalog, request, cancellationToken));

app.Run();

static async Task<IResult> RouteToAsync(
    string serverKey, McpClient client, McpToolCatalog catalog, RouteRequest request, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.ToolName))
        return Results.BadRequest("toolName is required.");

    // Cheap name lookup against the cached catalog, not orchestration logic: catches a typo'd
    // or wrong-route tool name with a clear 404 instead of an opaque protocol-level failure.
    var tools = await catalog.GetToolsAsync(forceRefresh: false, cancellationToken);
    if (!tools.Any(t => t.Server == serverKey && t.Name == request.ToolName))
        return Results.NotFound($"Tool '{request.ToolName}' is not exposed by the {serverKey} server.");

    CallToolResult result;
    try
    {
        result = await client.CallToolAsync(
            request.ToolName,
            request.Arguments ?? new Dictionary<string, object>(),
            cancellationToken: cancellationToken);
    }
    catch (McpException ex)
    {
        // Protocol-level failure (unreachable server, malformed request) - not the same thing
        // as a business-outcome error, which comes back as IsError below and is surfaced as-is.
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }

    var content = result.Content.OfType<TextContentBlock>().Select(c => c.Text).ToList();

    // MAF surfaces the tool's own error/success as-is - see "Failure handling" in
    // ARCHITECTURE.md's MAF Orchestrator section. It does not translate business-outcome
    // errors (e.g. StationAlreadyExistsException) into HTTP status codes itself; that
    // translation is the API layer's job once OpsApi actually calls through MAF.
    return Results.Ok(new RouteResult(result.IsError == true, content));
}

public class MafOptions
{
    public const string SectionName = "Maf";

    public string InternalServerUrl { get; set; } = default!;
    public string HaikuServerUrl { get; set; } = default!;
}

public record DiscoveredTool(string Server, string Name, string? Description);

public record RouteRequest(string ToolName, Dictionary<string, object>? Arguments);

public record RouteResult(bool IsError, IReadOnlyList<string> Content);
