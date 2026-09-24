using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;
using Telara.Domain.Exceptions;

namespace Telara.Domain.CQRS.Queries;

public class GetStationWorkflowQueryHandler(TelaraDbContext db)
    : IRequestHandler<GetStationWorkflowQuery, IReadOnlyList<Station>>
{
    public async ValueTask<IReadOnlyList<Station>> Handle(GetStationWorkflowQuery request, CancellationToken cancellationToken)
    {
        var stationsById = await db.Stations
            .AsNoTracking()
            .Where(s => s.FacilityId == request.FacilityId)
            .Include(s => s.Equipment)
                .ThenInclude(e => e.EquipmentType)
            .ToDictionaryAsync(s => s.StationId, cancellationToken);

        if (!stationsById.TryGetValue(request.StationId, out var current))
            throw new StationNotFoundException(request.FacilityId, request.StationId);

        // NextStationId cycles/multiple loading docks aren't DB-enforced yet (see ARCHITECTURE.md
        // Open Risks), so this walk guards itself with a visited set rather than trusting the chain
        // to terminate.
        var visited = new HashSet<string>();
        var ordered = new List<Station>();

        while (current is not null && visited.Add(current.StationId))
        {
            ordered.Add(current);

            if (current.IsLoadingDock || current.NextStationId is null)
                break;

            stationsById.TryGetValue(current.NextStationId, out current);
        }

        return ordered;
    }
}
