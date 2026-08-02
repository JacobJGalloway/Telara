using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Commands;

public record ClaimStationEquipmentLeaseCommand(
    string FacilityId,
    string StationId,
    string EquipmentId,
    int EquipmentTypeId,
    string InstanceId,
    TimeSpan LeaseDuration) : IRequest<ClaimStationEquipmentLeaseResult>;

public record ClaimStationEquipmentLeaseResult(
    string FacilityId,
    string StationId,
    string EquipmentId,
    EquipmentStatus Status,
    DateTime LeaseExpiresAtUtc);
