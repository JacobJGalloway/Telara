using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;
using Telara.Domain.Exceptions;

namespace Telara.Domain.CQRS.Commands;

public class RegisterStationEquipmentCommandHandler(TelaraDbContext db)
    : IRequestHandler<RegisterStationEquipmentCommand, RegisterStationEquipmentResult>
{
    public async ValueTask<RegisterStationEquipmentResult> Handle(RegisterStationEquipmentCommand request, CancellationToken cancellationToken)
    {
        var stationExists = await db.Stations.AnyAsync(
            s => s.FacilityId == request.FacilityId && s.StationId == request.StationId,
            cancellationToken);

        if (!stationExists)
            throw new StationNotFoundException(request.FacilityId, request.StationId);

        var alreadyRegistered = await db.StationEquipment.AnyAsync(
            e => e.FacilityId == request.FacilityId && e.StationId == request.StationId && e.EquipmentId == request.EquipmentId,
            cancellationToken);

        if (alreadyRegistered)
            throw new StationEquipmentAlreadyRegisteredException(request.FacilityId, request.StationId, request.EquipmentId);

        var equipment = new StationEquipment
        {
            FacilityId = request.FacilityId,
            StationId = request.StationId,
            EquipmentId = request.EquipmentId,
            EquipmentTypeId = request.EquipmentTypeId,
            Status = EquipmentStatus.Idle,
        };
        db.StationEquipment.Add(equipment);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Narrow race window, same shape as RegisterStationCommandHandler.
            throw new StationEquipmentAlreadyRegisteredException(request.FacilityId, request.StationId, request.EquipmentId);
        }

        return new RegisterStationEquipmentResult(
            equipment.FacilityId, equipment.StationId, equipment.EquipmentId, equipment.EquipmentTypeId, equipment.Status);
    }
}
