using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Commands;

public class RecordSensorReadingsCommandHandler(TelaraDbContext db) : IRequestHandler<RecordSensorReadingsCommand, bool>
{
    public async ValueTask<bool> Handle(RecordSensorReadingsCommand request, CancellationToken cancellationToken)
    {
        var equipment = await db.StationEquipment.FirstOrDefaultAsync(
            e => e.FacilityId == request.FacilityId && e.StationId == request.StationId && e.EquipmentId == request.EquipmentId,
            cancellationToken);

        if (equipment is null)
            return false;

        var now = DateTime.UtcNow;

        foreach (var reading in request.Readings)
        {
            db.SensorReadings.Add(new SensorReading
            {
                FacilityId = request.FacilityId,
                StationId = request.StationId,
                EquipmentId = request.EquipmentId,
                SensorId = reading.SensorId,
                ReadingType = reading.ReadingType,
                Value = reading.Value,
                ReadingAtUtc = now,
            });
        }

        equipment.LastSensorReadingUtc = now;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
