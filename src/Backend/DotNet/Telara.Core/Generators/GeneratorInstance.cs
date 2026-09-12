using Telara.Domain.Telemetry;

namespace Telara.Core.Generators;

public class GeneratorInstance(GeneratorInstanceConfig config, TelemetryKey key)
{
    public GeneratorInstanceConfig Config { get; } = config;
    public TelemetryKey Key { get; } = key;
    public DateTime? LastLeaseClaimUtc { get; set; }

    // Mutable simulation state, set live via OpsApi's /api/simulator/* control routes (same
    // process, in-memory - no MAF/domain write involved in setting the multiplier itself, only
    // in the output it goes on to produce). Multiplier of 1 is normal output; lower values
    // simulate a station running below its targeted rate for root-cause-finding scenarios.
    public decimal OutputRateMultiplier { get; set; } = 1m;
    public decimal CumulativeUnitsProduced { get; set; }
}
