namespace Telara.Domain.Telemetry;

public enum TelemetryLevel
{
    Station,
    Equipment,
    Sensor,
}

public readonly record struct TelemetryKey(string FacilityId, string StationId, TelemetryLevel Level, string TelemetryId);
