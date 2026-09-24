using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public record GetStationWorkflowQuery(string FacilityId, string StationId) : IRequest<IReadOnlyList<Station>>;
