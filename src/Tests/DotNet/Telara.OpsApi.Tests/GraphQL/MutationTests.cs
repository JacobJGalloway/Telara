using System.Net;
using System.Net.Http.Json;
using HotChocolate;
using Telara.Core.Maf;

namespace Telara.OpsApi.Tests.GraphQL;

public class MutationTests
{
    private static MafClient CreateMafClient(bool isError, string content)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { isError, content = new[] { content } }),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new MafClient(httpClient);
    }

    [Fact]
    public async Task RegisterStation_GreenPath_DeserializesResult()
    {
        var mafClient = CreateMafClient(isError: false,
            content: """{"facilityId":"F1","stationId":"S1","isLoadingDock":false,"predecessorStationIds":[]}""");

        var result = await Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", [], false, CancellationToken.None);

        Assert.Equal("S1", result.StationId);
    }

    [Fact]
    public async Task RegisterStation_MapsToConflict_WhenAlreadyRegistered()
    {
        var mafClient = CreateMafClient(isError: true, content: "Station S1 at facility F1 is already registered.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", [], false, CancellationToken.None));

        Assert.Equal("CONFLICT", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task RegisterStation_MapsToConflict_WhenPredecessorAlreadyLinked()
    {
        var mafClient = CreateMafClient(isError: true, content: "Station UPSTREAM at facility F1 cannot feed into a new successor - it already has one, or it is a loading dock.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", ["UPSTREAM"], false, CancellationToken.None));

        Assert.Equal("CONFLICT", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task RegisterStation_MapsToNotFound_WhenPredecessorMissing()
    {
        var mafClient = CreateMafClient(isError: true, content: "Station missing at facility F1 is not registered.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", ["missing"], false, CancellationToken.None));

        Assert.Equal("NOT_FOUND", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task RegisterStation_MapsToInternal_ForUnrecognizedFailure()
    {
        var mafClient = CreateMafClient(isError: true, content: "Something unexpected happened.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", [], false, CancellationToken.None));

        Assert.Equal("INTERNAL", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task RegisterStationEquipment_GreenPath_DeserializesResult()
    {
        var mafClient = CreateMafClient(isError: false,
            content: """{"facilityId":"F1","stationId":"S1","equipmentId":"EQ-1","equipmentTypeId":6,"status":"Idle"}""");

        var result = await Telara.OpsApi.GraphQL.Mutation.RegisterStationEquipment(mafClient, "F1", "S1", "EQ-1", 6, CancellationToken.None);

        Assert.Equal("EQ-1", result.EquipmentId);
    }

    [Fact]
    public async Task RegisterStationEquipment_MapsToNotFound_WhenStationMissing()
    {
        var mafClient = CreateMafClient(isError: true, content: "Station S1 at facility F1 is not registered.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStationEquipment(mafClient, "F1", "S1", "EQ-1", 6, CancellationToken.None));

        Assert.Equal("NOT_FOUND", Assert.Single(ex.Errors).Code);
    }
}
