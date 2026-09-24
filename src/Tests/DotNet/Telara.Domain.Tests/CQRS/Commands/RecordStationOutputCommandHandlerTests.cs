using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.Tests.CQRS.Commands;

public class RecordStationOutputCommandHandlerTests
{
    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_RecordsOutput_WhenStationExists()
    {
        await using var db = CreateContext();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1" });
        await db.SaveChangesAsync();
        var handler = new RecordStationOutputCommandHandler(db);

        var result = await handler.Handle(new RecordStationOutputCommand("F1", "S1", 42m), CancellationToken.None);

        Assert.True(result);
        var record = Assert.Single(db.StationOutputRecords);
        Assert.Equal("F1", record.FacilityId);
        Assert.Equal("S1", record.StationId);
        Assert.Equal(42m, record.UnitsProduced);
    }

    [Fact]
    public async Task Handle_AppendsRecords_RatherThanReplacing()
    {
        await using var db = CreateContext();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1" });
        await db.SaveChangesAsync();
        var handler = new RecordStationOutputCommandHandler(db);

        await handler.Handle(new RecordStationOutputCommand("F1", "S1", 10m), CancellationToken.None);
        await handler.Handle(new RecordStationOutputCommand("F1", "S1", 20m), CancellationToken.None);

        Assert.Equal(2, db.StationOutputRecords.Count());
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenStationDoesNotExist()
    {
        await using var db = CreateContext();
        var handler = new RecordStationOutputCommandHandler(db);

        var result = await handler.Handle(new RecordStationOutputCommand("F1", "missing", 10m), CancellationToken.None);

        Assert.False(result);
        Assert.Empty(db.StationOutputRecords);
    }
}
