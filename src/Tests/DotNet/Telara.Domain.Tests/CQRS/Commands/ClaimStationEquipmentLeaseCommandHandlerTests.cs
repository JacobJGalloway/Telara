using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Data;
using Telara.Domain.Exceptions;

namespace Telara.Domain.Tests.CQRS.Commands;

public class ClaimStationEquipmentLeaseCommandHandlerTests
{
    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ClaimStationEquipmentLeaseCommand Command(string instanceId = "instance-1") =>
        new("F1", "S1", "E1", EquipmentTypeId: 1, instanceId, LeaseDuration: TimeSpan.FromMinutes(5));

    [Fact]
    public async Task Handle_CreatesStationEquipment_WhenNoneExists()
    {
        await using var db = CreateContext();
        var handler = new ClaimStationEquipmentLeaseCommandHandler(db);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.Equal("E1", result.EquipmentId);
        Assert.Single(db.StationEquipment);
    }

    [Fact]
    public async Task Handle_ReclaimsLease_WhenHeldBySameInstance()
    {
        await using var db = CreateContext();
        var handler = new ClaimStationEquipmentLeaseCommandHandler(db);
        var command = Command();
        var first = await handler.Handle(command, CancellationToken.None);

        var second = await handler.Handle(command, CancellationToken.None);

        Assert.True(second.LeaseExpiresAtUtc >= first.LeaseExpiresAtUtc);
        Assert.Single(db.StationEquipment);
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenLeaseHeldByAnotherActiveInstance()
    {
        await using var db = CreateContext();
        var handler = new ClaimStationEquipmentLeaseCommandHandler(db);
        await handler.Handle(Command(instanceId: "instance-1"), CancellationToken.None);

        await Assert.ThrowsAsync<StationEquipmentAlreadyOperationalException>(
            () => handler.Handle(Command(instanceId: "instance-2"), CancellationToken.None).AsTask());
    }
}
