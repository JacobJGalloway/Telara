using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public record GetEquipmentTypesQuery : IRequest<IQueryable<EquipmentType>>;
