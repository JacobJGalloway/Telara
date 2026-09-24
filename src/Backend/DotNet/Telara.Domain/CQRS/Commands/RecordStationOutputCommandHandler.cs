using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Commands;

public class RecordStationOutputCommandHandler(TelaraDbContext db) : IRequestHandler<RecordStationOutputCommand, bool>
{
    public async ValueTask<bool> Handle(RecordStationOutputCommand request, CancellationToken cancellationToken)
    {
        var stationExists = await db.Stations.AnyAsync(
            s => s.FacilityId == request.FacilityId && s.StationId == request.StationId,
            cancellationToken);

        if (!stationExists)
            return false;

        db.StationOutputRecords.Add(new StationOutputRecord
        {
            FacilityId = request.FacilityId,
            StationId = request.StationId,
            UnitsProduced = request.UnitsProduced,
            RecordedAtUtc = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
