using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.Tests.CQRS.Commands;

public class RevokeRefreshTokenFamilyCommandHandlerTests
{
    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_RevokesEveryActiveTokenInFamily_AndReturnsTrue()
    {
        await using var db = CreateContext();
        var familyId = Guid.NewGuid();
        db.RefreshTokens.Add(new RefreshToken { UserId = 1, TokenHash = "hash-a", FamilyId = familyId, ExpiresAtUtc = DateTime.UtcNow.AddDays(14) });
        db.RefreshTokens.Add(new RefreshToken { UserId = 1, TokenHash = "hash-b", FamilyId = familyId, ExpiresAtUtc = DateTime.UtcNow.AddDays(14) });
        await db.SaveChangesAsync();
        var handler = new RevokeRefreshTokenFamilyCommandHandler(db);

        var result = await handler.Handle(new RevokeRefreshTokenFamilyCommand("hash-a"), CancellationToken.None);

        Assert.True(result);
        Assert.All(db.RefreshTokens, t => Assert.NotNull(t.RevokedAtUtc));
    }

    [Fact]
    public async Task Handle_LeavesOtherFamiliesUntouched()
    {
        await using var db = CreateContext();
        var targetFamily = Guid.NewGuid();
        var otherFamily = Guid.NewGuid();
        db.RefreshTokens.Add(new RefreshToken { UserId = 1, TokenHash = "hash-a", FamilyId = targetFamily, ExpiresAtUtc = DateTime.UtcNow.AddDays(14) });
        db.RefreshTokens.Add(new RefreshToken { UserId = 2, TokenHash = "hash-c", FamilyId = otherFamily, ExpiresAtUtc = DateTime.UtcNow.AddDays(14) });
        await db.SaveChangesAsync();
        var handler = new RevokeRefreshTokenFamilyCommandHandler(db);

        await handler.Handle(new RevokeRefreshTokenFamilyCommand("hash-a"), CancellationToken.None);

        Assert.Null((await db.RefreshTokens.SingleAsync(t => t.TokenHash == "hash-c")).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenTokenHashUnknown()
    {
        await using var db = CreateContext();
        var handler = new RevokeRefreshTokenFamilyCommandHandler(db);

        var result = await handler.Handle(new RevokeRefreshTokenFamilyCommand("missing-hash"), CancellationToken.None);

        Assert.False(result);
    }
}
