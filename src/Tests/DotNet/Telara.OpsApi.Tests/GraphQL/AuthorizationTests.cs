using System.Security.Claims;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telara.Core.Maf;
using Telara.Domain.Data;

namespace Telara.OpsApi.Tests.GraphQL;

// Every other test in this project calls resolvers as plain static methods, which never goes
// through HotChocolate's request pipeline - [Authorize]/[Authorize(Roles = ...)] is middleware
// around resolver execution, not something a direct method call exercises at all. These tests
// build a real IRequestExecutor so role-gating is actually proven, not just asserted in a doc.
public class AuthorizationTests
{
    private static async Task<IRequestExecutor> BuildExecutorAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TelaraDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddHttpClient<MafClient>(c => c.BaseAddress = new Uri("http://localhost/"));
        services.AddAuthorization();

        services
            .AddGraphQL()
            .AddOpsApiTypes()
            .AddFiltering()
            .AddSorting()
            .AddAuthorization();

        return await services.BuildServiceProvider().GetRequestExecutorAsync();
    }

    private static ClaimsPrincipal PrincipalWithRole(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], authenticationType: "test"));

    private const string RegisterStationMutation =
        """mutation { registerStation(facilityId: "F1", stationId: "S1", isLoadingDock: false) { stationId } }""";

    private const string RegisterStationEquipmentMutation =
        """mutation { registerStationEquipment(facilityId: "F1", stationId: "S1", equipmentId: "EQ-1", equipmentTypeId: 1) { equipmentId } }""";

    [Theory]
    [InlineData("Shift Manager")]
    [InlineData("Plant Director")]
    public async Task RegisterStation_Denied_ForNonStationSupervisorRoles(string role)
    {
        var executor = await BuildExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument(RegisterStationMutation)
            .SetGlobalState(nameof(ClaimsPrincipal), PrincipalWithRole(role))
            .Build();

        var result = await executor.ExecuteAsync(request);
        var queryResult = result.ExpectOperationResult();

        Assert.Contains(queryResult.Errors ?? [], e => e.Code == "AUTH_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task RegisterStation_NotDeniedByAuthorization_ForStationSupervisor()
    {
        var executor = await BuildExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument(RegisterStationMutation)
            .SetGlobalState(nameof(ClaimsPrincipal), PrincipalWithRole("Station Supervisor"))
            .Build();

        var result = await executor.ExecuteAsync(request);
        var queryResult = result.ExpectOperationResult();

        Assert.DoesNotContain(queryResult.Errors ?? [], e => e.Code == "AUTH_NOT_AUTHORIZED");
    }

    [Theory]
    [InlineData("Shift Manager")]
    [InlineData("Plant Director")]
    public async Task RegisterStationEquipment_Denied_ForNonStationSupervisorRoles(string role)
    {
        var executor = await BuildExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument(RegisterStationEquipmentMutation)
            .SetGlobalState(nameof(ClaimsPrincipal), PrincipalWithRole(role))
            .Build();

        var result = await executor.ExecuteAsync(request);
        var queryResult = result.ExpectOperationResult();

        Assert.Contains(queryResult.Errors ?? [], e => e.Code == "AUTH_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task RegisterStationEquipment_NotDeniedByAuthorization_ForStationSupervisor()
    {
        var executor = await BuildExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument(RegisterStationEquipmentMutation)
            .SetGlobalState(nameof(ClaimsPrincipal), PrincipalWithRole("Station Supervisor"))
            .Build();

        var result = await executor.ExecuteAsync(request);
        var queryResult = result.ExpectOperationResult();

        Assert.DoesNotContain(queryResult.Errors ?? [], e => e.Code == "AUTH_NOT_AUTHORIZED");
    }

    [Theory]
    [InlineData("Station Supervisor")]
    [InlineData("Shift Manager")]
    [InlineData("Plant Director")]
    public async Task GetStations_NotDeniedByAuthorization_ForAnyRole(string role)
    {
        var executor = await BuildExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""query { stations(facilityId: "F1") { stationId } }""")
            .SetGlobalState(nameof(ClaimsPrincipal), PrincipalWithRole(role))
            .Build();

        var result = await executor.ExecuteAsync(request);
        var queryResult = result.ExpectOperationResult();

        Assert.DoesNotContain(queryResult.Errors ?? [], e => e.Code == "AUTH_NOT_AUTHORIZED");
    }
}
