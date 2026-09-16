using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.Tests.CQRS.Commands;

public class ValidateAndConsumeRefreshTokenCommandHandlerTests
{
    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_Succeeds_AndConsumesToken_OnFirstUse()
    {
        await using var db = CreateContext();
        var familyId = Guid.NewGuid();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = 1,
            TokenHash = "hash-a",
            FamilyId = familyId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
        });
        await db.SaveChangesAsync();
        var handler = new ValidateAndConsumeRefreshTokenCommandHandler(db);

        var result = await handler.Handle(new ValidateAndConsumeRefreshTokenCommand("hash-a"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.UserId);
        Assert.Equal(familyId, result.FamilyId);
        Assert.False(result.ReuseDetected);
        Assert.NotNull((await db.RefreshTokens.SingleAsync(t => t.TokenHash == "hash-a")).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_Fails_WhenTokenHashUnknown()
    {
        await using var db = CreateContext();
        var handler = new ValidateAndConsumeRefreshTokenCommandHandler(db);

        var result = await handler.Handle(new ValidateAndConsumeRefreshTokenCommand("missing-hash"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Null(result.UserId);
        Assert.False(result.ReuseDetected);
    }

    [Fact]
    public async Task Handle_Fails_WhenTokenExpired()
    {
        await using var db = CreateContext();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = 1,
            TokenHash = "hash-a",
            FamilyId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-20),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-6),
        });
        await db.SaveChangesAsync();
        var handler = new ValidateAndConsumeRefreshTokenCommandHandler(db);

        var result = await handler.Handle(new ValidateAndConsumeRefreshTokenCommand("hash-a"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(result.ReuseDetected);
    }

    // The reuse/theft case this handler exists for: a token that was already rotated out
    // (RevokedAtUtc set) gets presented again, meaning either it was stolen or we're replaying
    // an old client state - either way the whole family must die, not just this one token.
    [Fact]
    public async Task Handle_DetectsReuse_AndRevokesEntireFamily_WhenAlreadyConsumedTokenIsPresentedAgain()
    {
        await using var db = CreateContext();
        var familyId = Guid.NewGuid();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = 1,
            TokenHash = "hash-a",
            FamilyId = familyId,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
            RevokedAtUtc = DateTime.UtcNow.AddMinutes(-5),
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = 1,
            TokenHash = "hash-b",
            FamilyId = familyId,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
        });
        await db.SaveChangesAsync();
        var handler = new ValidateAndConsumeRefreshTokenCommandHandler(db);

        var result = await handler.Handle(new ValidateAndConsumeRefreshTokenCommand("hash-a"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.ReuseDetected);
        Assert.Equal(1, result.UserId);
        Assert.Equal(familyId, result.FamilyId);
        Assert.NotNull((await db.RefreshTokens.SingleAsync(t => t.TokenHash == "hash-b")).RevokedAtUtc);
    }
}
