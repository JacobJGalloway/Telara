using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public record GetStationsQuery(string FacilityId) : IRequest<IReadOnlyList<Station>>;
