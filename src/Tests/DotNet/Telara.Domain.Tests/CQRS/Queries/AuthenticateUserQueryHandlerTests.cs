using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Queries;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.Tests.CQRS.Queries;

public class AuthenticateUserQueryHandlerTests
{
    private static readonly PasswordHasher<User> Hasher = new();

    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static User SeededUser(string email, string password)
    {
        var user = new User { Email = email, FirstName = "Ada", LastName = "Lovelace", AssignedStationId = "S1" };
        user.PasswordHash = Hasher.HashPassword(user, password);
        return user;
    }

    [Fact]
    public async Task Handle_ReturnsUser_WhenPasswordMatches()
    {
        await using var db = CreateContext();
        db.Users.Add(SeededUser("ada@telara.dev", "correct-horse"));
        await db.SaveChangesAsync();
        var handler = new AuthenticateUserQueryHandler(db);

        var result = await handler.Handle(new AuthenticateUserQuery("ada@telara.dev", "correct-horse"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ada@telara.dev", result!.Email);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenPasswordDoesNotMatch()
    {
        await using var db = CreateContext();
        db.Users.Add(SeededUser("ada@telara.dev", "correct-horse"));
        await db.SaveChangesAsync();
        var handler = new AuthenticateUserQueryHandler(db);

        var result = await handler.Handle(new AuthenticateUserQuery("ada@telara.dev", "wrong-password"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenEmailDoesNotExist()
    {
        await using var db = CreateContext();
        var handler = new AuthenticateUserQueryHandler(db);

        var result = await handler.Handle(new AuthenticateUserQuery("missing@telara.dev", "anything"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_IncludesRole_WhenUserHasOne()
    {
        await using var db = CreateContext();
        var user = SeededUser("ada@telara.dev", "correct-horse");
        user.Role = new Role { Name = "Supervisor" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var handler = new AuthenticateUserQueryHandler(db);

        var result = await handler.Handle(new AuthenticateUserQuery("ada@telara.dev", "correct-horse"), CancellationToken.None);

        Assert.Equal("Supervisor", result!.Role!.Name);
    }
}
