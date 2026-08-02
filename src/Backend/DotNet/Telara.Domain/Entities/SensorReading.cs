using Telara.Domain.Entities.Interfaces;

namespace Telara.Domain.Entities;

public class SensorReading : ISensorReading
{
    public long Id { get; set; }
    public string FacilityId { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public string EquipmentId { get; set; } = default!;
    public string SensorId { get; set; } = default!;
    public string ReadingType { get; set; } = default!;
    public decimal Value { get; set; }
    public DateTime ReadingAtUtc { get; set; }
    public StationEquipment? StationEquipment { get; set; }
}
