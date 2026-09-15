using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telara.Domain.Data;

namespace Telara.OpsApi.Tests;

// Same reasoning as Telara.Mcp.Internal.Tests' fixture - wires the real Mediator DI pipeline
// (Telara.OpsApi's own generated AddMediator()) over an InMemory TelaraDbContext, so Query tests
// exercise the actual handlers.
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
