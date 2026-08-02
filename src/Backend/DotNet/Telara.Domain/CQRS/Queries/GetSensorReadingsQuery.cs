using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public record GetSensorReadingsQuery : IRequest<IQueryable<SensorReading>>;
