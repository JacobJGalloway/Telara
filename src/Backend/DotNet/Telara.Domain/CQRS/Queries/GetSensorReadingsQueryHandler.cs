using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public class GetSensorReadingsQueryHandler(TelaraDbContext db)
    : IRequestHandler<GetSensorReadingsQuery, IQueryable<SensorReading>>
{
    public ValueTask<IQueryable<SensorReading>> Handle(GetSensorReadingsQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(db.SensorReadings.AsNoTracking());
}
