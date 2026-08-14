using Mediator;

namespace Telara.Domain.CQRS.Commands;

public record RegisterStationCommand(string FacilityId, string StationId) : IRequest<RegisterStationResult>;

public record RegisterStationResult(string FacilityId, string StationId);
