using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;
using Telara.Domain.Exceptions;

namespace Telara.Domain.CQRS.Commands;

public class RegisterStationCommandHandler(TelaraDbContext db)
    : IRequestHandler<RegisterStationCommand, RegisterStationResult>
{
    public async ValueTask<RegisterStationResult> Handle(RegisterStationCommand request, CancellationToken cancellationToken)
    {
        var exists = await db.Stations.AnyAsync(
            s => s.FacilityId == request.FacilityId && s.StationId == request.StationId,
            cancellationToken);

        if (exists)
            throw new StationAlreadyExistsException(request.FacilityId, request.StationId);

        db.Stations.Add(new Station
        {
            FacilityId = request.FacilityId,
            StationId = request.StationId,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Narrow race window, same shape as ClaimStationEquipmentLeaseCommandHandler: another
            // caller's insert won between our AnyAsync check and this save.
            throw new StationAlreadyExistsException(request.FacilityId, request.StationId);
        }

        return new RegisterStationResult(request.FacilityId, request.StationId);
    }
}
