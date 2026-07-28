using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public record GetStationEquipmentReadingsQuery : IRequest<IQueryable<StationEquipmentReading>>;
