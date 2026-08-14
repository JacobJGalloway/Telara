using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Commands;

public record RegisterStationEquipmentCommand(
    string FacilityId,
    string StationId,
    string EquipmentId,
    int EquipmentTypeId) : IRequest<RegisterStationEquipmentResult>;

public record RegisterStationEquipmentResult(
    string FacilityId,
    string StationId,
    string EquipmentId,
    int EquipmentTypeId,
    EquipmentStatus Status);
