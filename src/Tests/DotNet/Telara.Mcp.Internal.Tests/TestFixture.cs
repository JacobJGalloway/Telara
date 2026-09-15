using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telara.Domain.Data;

namespace Telara.Mcp.Internal.Tests;

// Wires the real Mediator DI pipeline (same AddMediator() call as Telara.Mcp.Internal's own
// Program.cs) over an InMemory TelaraDbContext, so tool tests exercise the actual handlers rather
// than a stand-in - these are integration tests of the MCP tool -> Mediator -> handler seam, not
// unit tests of the tool method's own (mostly nonexistent) logic.
public static class TestFixture
{
    public static (ISender Sender, TelaraDbContext Db) Create()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TelaraDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        var provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<ISender>(), provider.GetRequiredService<TelaraDbContext>());
    }
}
