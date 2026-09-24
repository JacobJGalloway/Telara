using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public class GetStationOutputQueryHandler(TelaraDbContext db)
    : IRequestHandler<GetStationOutputQuery, IQueryable<StationOutputRecord>>
{
    public ValueTask<IQueryable<StationOutputRecord>> Handle(GetStationOutputQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(db.StationOutputRecords.AsNoTracking());
}
