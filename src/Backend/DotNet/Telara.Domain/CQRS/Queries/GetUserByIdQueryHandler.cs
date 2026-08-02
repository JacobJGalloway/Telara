using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public class GetUserByIdQueryHandler(TelaraDbContext db) : IRequestHandler<GetUserByIdQuery, User?>
{
    public async ValueTask<User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken) =>
        await
        db.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
}
