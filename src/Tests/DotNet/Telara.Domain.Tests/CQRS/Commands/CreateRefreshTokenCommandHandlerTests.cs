using Microsoft.EntityFrameworkCore;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Data;

namespace Telara.Domain.Tests.CQRS.Commands;

public class CreateRefreshTokenCommandHandlerTests
{
    private static TelaraDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TelaraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_PersistsToken_AndReturnsItsId()
    {
        await using var db = CreateContext();
        var handler = new CreateRefreshTokenCommandHandler(db);
        var familyId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddDays(14);

        var id = await handler.Handle(new CreateRefreshTokenCommand(1, "hashed-token", familyId, expiresAtUtc), CancellationToken.None);

        var token = await db.RefreshTokens.SingleAsync(t => t.Id == id);
        Assert.Equal(1, token.UserId);
        Assert.Equal("hashed-token", token.TokenHash);
        Assert.Equal(familyId, token.FamilyId);
        Assert.Equal(expiresAtUtc, token.ExpiresAtUtc);
        Assert.Null(token.RevokedAtUtc);
    }
}
