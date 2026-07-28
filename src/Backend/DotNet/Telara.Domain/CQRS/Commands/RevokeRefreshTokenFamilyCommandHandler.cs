using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;

namespace Telara.Domain.CQRS.Commands;

public class RevokeRefreshTokenFamilyCommandHandler(TelaraDbContext db) : IRequestHandler<RevokeRefreshTokenFamilyCommand, bool>
{
    public async ValueTask<bool> Handle(RevokeRefreshTokenFamilyCommand request, CancellationToken cancellationToken)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == request.TokenHash, cancellationToken);
        if (token is null)
            return false;

        var family = await db.RefreshTokens
            .Where(t => t.FamilyId == token.FamilyId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var familyToken in family)
            familyToken.RevokedAtUtc = now;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
