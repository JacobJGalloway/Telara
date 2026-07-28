using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public class GetStationEquipmentReadingsQueryHandler(TelaraDbContext db)
    : IRequestHandler<GetStationEquipmentReadingsQuery, IQueryable<StationEquipmentReading>>
{
    public ValueTask<IQueryable<StationEquipmentReading>> Handle(GetStationEquipmentReadingsQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(db.StationEquipmentReadings.AsNoTracking());
}
