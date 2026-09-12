namespace Telara.Domain.Entities.Interfaces;

public interface IStationOutputRecord
{
    long Id { get; set; }
    string FacilityId { get; set; }
    string StationId { get; set; }
    DateTime RecordedAtUtc { get; set; }
    decimal UnitsProduced { get; set; }
}
