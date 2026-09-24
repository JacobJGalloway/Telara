using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public record GetStationOutputQuery : IRequest<IQueryable<StationOutputRecord>>;
