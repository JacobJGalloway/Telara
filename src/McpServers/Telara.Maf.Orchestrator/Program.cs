var builder = WebApplication.CreateBuilder(args);

// MAF orchestrator, stateless router only this sprint - see "MAF Orchestrator (This Sprint)"
// in ARCHITECTURE.md. Sits in front of the two MCP servers and is the layer the GraphQL
// resolvers and the generator's write path call through, instead of either calling MCP
// tools directly. Not itself an MCP server: its callers are internal .NET code, not an LLM
// agent, so it exposes a plain routing API and acts as an MCP *client* to the two servers.
builder.Services.Configure<MafOptions>(builder.Configuration.GetSection(MafOptions.SectionName));

var app = builder.Build();

// TODO(1.2): implement the routing rule from ARCHITECTURE.md - reads (sensor/Station/
// StationEquipment lookups) go to MafOptions.InternalServerUrl; operational/write calls
// (registration, lease claims, sensor writes) go to MafOptions.HaikuServerUrl. Each route
// below is a hole for that MCP client call, not implemented yet - scaffolding only.

app.MapPost("/route/read", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
app.MapPost("/route/operational", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

public class MafOptions
{
    public const string SectionName = "Maf";

    public string InternalServerUrl { get; set; } = default!;
    public string HaikuServerUrl { get; set; } = default!;
}
