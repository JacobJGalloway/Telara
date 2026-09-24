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

        var predecessorIds = request.PredecessorStationIds.Distinct().ToList();
        var predecessors = new List<Station>();

        foreach (var predecessorId in predecessorIds)
        {
            var predecessor = await db.Stations.SingleOrDefaultAsync(
                s => s.FacilityId == request.FacilityId && s.StationId == predecessorId,
                cancellationToken);

            if (predecessor is null)
                throw new StationNotFoundException(request.FacilityId, predecessorId);

            // A predecessor can only feed into one successor (NextStationId is a single
            // self-referencing FK - see RegisterStationCommand), and a loading dock is a line's
            // terminal node by definition, so neither can be handed a new successor here.
            if (predecessor.IsLoadingDock || predecessor.NextStationId is not null)
                throw new PredecessorLinkConflictException(request.FacilityId, predecessorId);

            predecessors.Add(predecessor);
        }

        db.Stations.Add(new Station
        {
            FacilityId = request.FacilityId,
            StationId = request.StationId,
            IsLoadingDock = request.IsLoadingDock,
        });

        foreach (var predecessor in predecessors)
            predecessor.NextStationId = request.StationId;

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

        return new RegisterStationResult(request.FacilityId, request.StationId, request.IsLoadingDock, predecessorIds);
    }
}
