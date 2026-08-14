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

// TODO(1.2): implement the routing rule from ARCHITECTURE.md - reads (sensor/Station/
// StationEquipment lookups) go to the Internal Functionality Server; operational/write calls
// (registration, lease claims, sensor writes) go to the Claude Haiku Server. These stay
// stubbed 501s, not yet dispatching through McpServerClientRegistry, until the routing logic
// itself is implemented.

app.MapPost("/route/read", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
app.MapPost("/route/operational", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

public class MafOptions
{
    public const string SectionName = "Maf";

    public string InternalServerUrl { get; set; } = default!;
    public string HaikuServerUrl { get; set; } = default!;
}

public record DiscoveredTool(string Server, string Name, string? Description);
