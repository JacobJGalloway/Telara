using Telara.Domain.Telemetry;

namespace Telara.Core.Generators;

public sealed class DuplicateTelemetryKeyException(TelemetryKey duplicateKey)
    : Exception($"Duplicate TelemetryKey in generator configuration: {duplicateKey}")
{
    public TelemetryKey DuplicateKey { get; } = duplicateKey;
}
