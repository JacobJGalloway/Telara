var builder = WebApplication.CreateBuilder(args);

// Claude Haiku MCP Server, per the MCP Server Topology decision in DECISIONS.md: linear,
// high-throughput workflows (ingestion, registration hand-off). Haiku owns *workflow*, not
// domain contract shape - it calls back into the Internal Functionality Server's tools to
// format raw data into domain models and to persist it, so it never needs to know the
// domain model's structure directly. See "MAF Orchestrator (This Sprint)" in ARCHITECTURE.md.
builder.Services.Configure<HaikuOptions>(builder.Configuration.GetSection(HaikuOptions.SectionName));

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// TODO(1.2): register an MCP client here (McpClientFactory + HttpClientTransport) pointed at
// HaikuOptions.InternalServerUrl, once the first workflow tool (ingestion/registration) is
// implemented. Left unwired for now - this is scaffolding only.

var app = builder.Build();

// MAF routes here once it exists (see "MAF Orchestrator (This Sprint)" in ARCHITECTURE.md).
// Until then, this server has no caller wired up on purpose.
app.MapMcp("/mcp");

app.Run();

public class HaikuOptions
{
    public const string SectionName = "Haiku";

    public string InternalServerUrl { get; set; } = default!;
}
