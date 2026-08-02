using Telara.Domain.Telemetry;

namespace Telara.Domain.Exceptions;

public sealed class StationEquipmentAlreadyOperationalException(TelemetryKey conflictingKey)
    : Exception($"StationEquipment {conflictingKey.TelemetryId} at station {conflictingKey.StationId} is already leased by another active instance.")
{
    public TelemetryKey ConflictingKey { get; } = conflictingKey;
}
