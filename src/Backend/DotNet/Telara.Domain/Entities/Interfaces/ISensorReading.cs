namespace Telara.Domain.Entities.Interfaces;

public interface ISensorReading
{
    long Id { get; set; }
    string FacilityId { get; set; }
    string StationId { get; set; }
    string EquipmentId { get; set; }
    string SensorId { get; set; }
    string ReadingType { get; set; }
    decimal Value { get; set; }
    DateTime ReadingAtUtc { get; set; }
}
