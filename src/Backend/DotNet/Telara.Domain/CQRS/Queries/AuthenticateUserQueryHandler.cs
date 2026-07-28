using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public class AuthenticateUserQueryHandler(TelaraDbContext db) : IRequestHandler<AuthenticateUserQuery, User?>
{
    private static readonly PasswordHasher<User> Hasher = new();

    public async ValueTask<User?> Handle(AuthenticateUserQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
            return null;

        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded
            ? user
            : null;
    }
}
