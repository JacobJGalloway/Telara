using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Data;
using Telara.Domain.Entities;
using Telara.Domain.Exceptions;

namespace Telara.Domain.Tests.CQRS.Commands;

public class RegisterStationCommandHandlerTests
{
    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_RegistersStation_WithNoPredecessors()
    {
        await using var db = CreateContext();
        var handler = new RegisterStationCommandHandler(db);

        var result = await handler.Handle(new RegisterStationCommand("F1", "S1", [], IsLoadingDock: false), CancellationToken.None);

        Assert.Equal("S1", result.StationId);
        var station = Assert.Single(db.Stations);
        Assert.Null(station.NextStationId);
        Assert.False(station.IsLoadingDock);
    }

    [Fact]
    public async Task Handle_RegistersLoadingDock()
    {
        await using var db = CreateContext();
        var handler = new RegisterStationCommandHandler(db);

        await handler.Handle(new RegisterStationCommand("F1", "S1", [], IsLoadingDock: true), CancellationToken.None);

        Assert.True(Assert.Single(db.Stations).IsLoadingDock);
    }

    [Fact]
    public async Task Handle_Throws_WhenStationAlreadyExists()
    {
        await using var db = CreateContext();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1" });
        await db.SaveChangesAsync();
        var handler = new RegisterStationCommandHandler(db);

        await Assert.ThrowsAsync<StationAlreadyExistsException>(() =>
            handler.Handle(new RegisterStationCommand("F1", "S1", [], IsLoadingDock: false), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_LinksSinglePredecessor_ToNewStation()
    {
        await using var db = CreateContext();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "UPSTREAM" });
        await db.SaveChangesAsync();
        var handler = new RegisterStationCommandHandler(db);

        await handler.Handle(new RegisterStationCommand("F1", "DOWNSTREAM", ["UPSTREAM"], IsLoadingDock: false), CancellationToken.None);

        var upstream = await db.Stations.SingleAsync(s => s.StationId == "UPSTREAM");
        Assert.Equal("DOWNSTREAM", upstream.NextStationId);
    }

    [Fact]
    public async Task Handle_FunnelsMultiplePredecessors_IntoOneSuccessor()
    {
        await using var db = CreateContext();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "LEFT" });
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "RIGHT" });
        await db.SaveChangesAsync();
        var handler = new RegisterStationCommandHandler(db);

        await handler.Handle(new RegisterStationCommand("F1", "JOIN", ["LEFT", "RIGHT"], IsLoadingDock: false), CancellationToken.None);

        Assert.Equal("JOIN", (await db.Stations.SingleAsync(s => s.StationId == "LEFT")).NextStationId);
        Assert.Equal("JOIN", (await db.Stations.SingleAsync(s => s.StationId == "RIGHT")).NextStationId);
    }

    [Fact]
    public async Task Handle_Throws_WhenPredecessorDoesNotExist()
    {
        await using var db = CreateContext();
        var handler = new RegisterStationCommandHandler(db);

        await Assert.ThrowsAsync<StationNotFoundException>(() =>
            handler.Handle(new RegisterStationCommand("F1", "S1", ["missing"], IsLoadingDock: false), CancellationToken.None).AsTask());
        Assert.Empty(db.Stations);
    }

    [Fact]
    public async Task Handle_Throws_WhenPredecessorAlreadyHasSuccessor()
    {
        await using var db = CreateContext();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "UPSTREAM", NextStationId = "OTHER" });
        await db.SaveChangesAsync();
        var handler = new RegisterStationCommandHandler(db);

        await Assert.ThrowsAsync<PredecessorLinkConflictException>(() =>
            handler.Handle(new RegisterStationCommand("F1", "S1", ["UPSTREAM"], IsLoadingDock: false), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_Throws_WhenPredecessorIsLoadingDock()
    {
        await using var db = CreateContext();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "DOCK", IsLoadingDock = true });
        await db.SaveChangesAsync();
        var handler = new RegisterStationCommandHandler(db);

        await Assert.ThrowsAsync<PredecessorLinkConflictException>(() =>
            handler.Handle(new RegisterStationCommand("F1", "S1", ["DOCK"], IsLoadingDock: false), CancellationToken.None).AsTask());
    }
}
