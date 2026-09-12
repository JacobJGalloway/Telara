using Telara.Domain.Entities.Interfaces;

namespace Telara.Domain.Entities;

// A shift-report-style snapshot of cumulative units produced at a Station, not a sensor
// reading - kept as its own table (rather than riding SensorReading's ReadingType) so it can
// later support mid-shift recordings at whatever granularity is useful without conflating
// "what a sensor measured" with "what the station actually produced."
public class StationOutputRecord : IStationOutputRecord
{
    public long Id { get; set; }
    public string FacilityId { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public DateTime RecordedAtUtc { get; set; }
    public decimal UnitsProduced { get; set; }
    public Station? Station { get; set; }
}
