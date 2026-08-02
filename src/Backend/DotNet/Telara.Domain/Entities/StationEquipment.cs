using Telara.Domain.Entities.Interfaces;

namespace Telara.Domain.Entities;

public class StationEquipment : IStationEquipment
{
    public string FacilityId { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public string EquipmentId { get; set; } = default!;
    public int EquipmentTypeId { get; set; }
    public EquipmentStatus Status { get; set; }
    public DateTime? LastSensorReadingUtc { get; set; }
    public string? ActiveInstanceId { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public Station? Station { get; set; }
    public EquipmentType? EquipmentType { get; set; }
}
