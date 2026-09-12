using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Queries;
using Telara.Domain.Data;
using Telara.Domain.Entities;
using Telara.Domain.Exceptions;

namespace Telara.Domain.Tests.CQRS.Queries;

public class GetStationWorkflowQueryHandlerTests
{
    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void SeedLine(TelaraDbContext db, string facilityId = "F1")
    {
        db.Stations.AddRange(
            new Station { FacilityId = facilityId, StationId = "S1", NextStationId = "S2" },
            new Station { FacilityId = facilityId, StationId = "S2", NextStationId = "S3" },
            new Station { FacilityId = facilityId, StationId = "S3", NextStationId = null, IsLoadingDock = true });
        db.SaveChanges();
    }

    [Fact]
    public async Task Handle_ReturnsStationsInOrder_TerminatingAtLoadingDock()
    {
        await using var db = CreateContext();
        SeedLine(db);
        var handler = new GetStationWorkflowQueryHandler(db);

        var result = await handler.Handle(new GetStationWorkflowQuery("F1", "S1"), CancellationToken.None);

        Assert.Equal(["S1", "S2", "S3"], result.Select(s => s.StationId));
        Assert.True(result[^1].IsLoadingDock);
    }

    [Fact]
    public async Task Handle_ReturnsSingleStation_WhenStartingStationIsLoadingDock()
    {
        await using var db = CreateContext();
        SeedLine(db);
        var handler = new GetStationWorkflowQueryHandler(db);

        var result = await handler.Handle(new GetStationWorkflowQuery("F1", "S3"), CancellationToken.None);

        Assert.Equal(["S3"], result.Select(s => s.StationId));
    }

    [Fact]
    public async Task Handle_StopsAtCycle_RatherThanLoopingForever()
    {
        await using var db = CreateContext();
        db.Stations.AddRange(
            new Station { FacilityId = "F1", StationId = "S1", NextStationId = "S2" },
            new Station { FacilityId = "F1", StationId = "S2", NextStationId = "S1" });
        await db.SaveChangesAsync();
        var handler = new GetStationWorkflowQueryHandler(db);

        var result = await handler.Handle(new GetStationWorkflowQuery("F1", "S1"), CancellationToken.None);

        Assert.Equal(["S1", "S2"], result.Select(s => s.StationId));
    }

    [Fact]
    public async Task Handle_Throws_WhenStartingStationDoesNotExist()
    {
        await using var db = CreateContext();
        SeedLine(db);
        var handler = new GetStationWorkflowQueryHandler(db);

        await Assert.ThrowsAsync<StationNotFoundException>(
            () => handler.Handle(new GetStationWorkflowQuery("F1", "missing"), CancellationToken.None).AsTask());
    }
}
