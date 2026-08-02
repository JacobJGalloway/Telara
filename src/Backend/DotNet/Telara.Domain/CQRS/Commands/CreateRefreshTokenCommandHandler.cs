using Mediator;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Commands;

public class CreateRefreshTokenCommandHandler(TelaraDbContext db) : IRequestHandler<CreateRefreshTokenCommand, long>
{
    public async ValueTask<long> Handle(CreateRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var token = new RefreshToken
        {
            UserId = request.UserId,
            TokenHash = request.TokenHash,
            FamilyId = request.FamilyId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = request.ExpiresAtUtc,
        };

        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        return token.Id;
    }
}
