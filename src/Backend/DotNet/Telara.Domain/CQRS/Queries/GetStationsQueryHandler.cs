using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public class GetStationsQueryHandler(TelaraDbContext db) : IRequestHandler<GetStationsQuery, IReadOnlyList<Station>>
{
    public async ValueTask<IReadOnlyList<Station>> Handle(GetStationsQuery request, CancellationToken cancellationToken) =>
        await db.Stations
            .AsNoTracking()
            .Where(s => s.FacilityId == request.FacilityId)
            .ToListAsync(cancellationToken);
}
