using Mediator;

namespace Telara.Domain.CQRS.Commands;

public record RecordStationOutputCommand(string FacilityId, string StationId, decimal UnitsProduced) : IRequest<bool>;
