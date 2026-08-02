namespace Telara.Domain.Entities.Interfaces;

public interface IStationEquipment
{
    string FacilityId { get; set; }
    string StationId { get; set; }
    string EquipmentId { get; set; }
    int EquipmentTypeId { get; set; }
    EquipmentStatus Status { get; set; }
    DateTime? LastSensorReadingUtc { get; set; }
    string? ActiveInstanceId { get; set; }
    DateTime? LeaseExpiresAtUtc { get; set; }
}
