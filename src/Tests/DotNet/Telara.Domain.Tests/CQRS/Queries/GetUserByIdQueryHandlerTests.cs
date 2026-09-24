using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Queries;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.Tests.CQRS.Queries;

public class GetUserByIdQueryHandlerTests
{
    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_ReturnsUser_WhenIdExists()
    {
        await using var db = CreateContext();
        var user = new User { Email = "ada@telara.dev", FirstName = "Ada", LastName = "Lovelace", AssignedStationId = "S1", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new GetUserByIdQueryHandler(db);
        var result = await handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ada@telara.dev", result!.Email);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenIdDoesNotExist()
    {
        await using var db = CreateContext();
        var handler = new GetUserByIdQueryHandler(db);

        var result = await handler.Handle(new GetUserByIdQuery(999), CancellationToken.None);

        Assert.Null(result);
    }
}
