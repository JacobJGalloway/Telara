using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TelaraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TelaraOps")));

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

// No-LLM MCP server: parameterized domain-data/data-slice lookups only, per the MCP Server
// Topology decision in DECISIONS.md. Tools wrap existing Telara.Domain CQRS handlers rather
// than reimplementing logic here — see "Business Logic Placement" in DECISIONS.md.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// MAF routes here once it exists (see "MAF Orchestrator (This Sprint)" in ARCHITECTURE.md).
// Until then, this server has no caller wired up on purpose - it runs standalone so its
// tools can be exercised directly (e.g. via an MCP inspector) while MAF isn't built yet.
app.MapMcp("/mcp");

app.Run();
