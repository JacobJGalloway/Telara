using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;

namespace Telara.Domain.CQRS.Commands;

public class ValidateAndConsumeRefreshTokenCommandHandler(TelaraDbContext db)
    : IRequestHandler<ValidateAndConsumeRefreshTokenCommand, RefreshTokenValidationResult>
{
    public async ValueTask<RefreshTokenValidationResult> Handle(ValidateAndConsumeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == request.TokenHash, cancellationToken);
        if (token is null)
            return new RefreshTokenValidationResult(false, null, null, false);

        if (token.RevokedAtUtc is not null)
        {
            // A previously-rotated-out token being presented again means it was stolen and used
            // by someone else (or by us after the legitimate rotation) - kill the whole chain.
            var family = await db.RefreshTokens
                .Where(t => t.FamilyId == token.FamilyId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var familyToken in family)
                familyToken.RevokedAtUtc = now;

            await db.SaveChangesAsync(cancellationToken);
            return new RefreshTokenValidationResult(false, token.UserId, token.FamilyId, true);
        }

        if (token.ExpiresAtUtc < DateTime.UtcNow)
            return new RefreshTokenValidationResult(false, null, null, false);

        token.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new RefreshTokenValidationResult(true, token.UserId, token.FamilyId, false);
    }
}
