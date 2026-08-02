using Telara.Domain.Telemetry;

namespace Telara.Core.Generators;

public class GeneratorInstance(GeneratorInstanceConfig config, TelemetryKey key)
{
    public GeneratorInstanceConfig Config { get; } = config;
    public TelemetryKey Key { get; } = key;
    public DateTime? LastLeaseClaimUtc { get; set; }
}
