namespace Telara.Core.Generators;

public class GeneratorInstanceConfig
{
    public string InstanceId { get; set; } = default!;
    public string FacilityId { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public string EquipmentId { get; set; } = default!;
    public int EquipmentTypeId { get; set; }
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan GenerationInterval { get; set; } = TimeSpan.FromMinutes(15);
    public List<SensorConfig> Sensors { get; set; } = [];

    // Baseline units produced per generation tick at StationId, before the simulator's
    // OutputRateMultiplier is applied - 0 (default) means this instance doesn't simulate
    // station output at all, only sensor telemetry.
    public decimal BaseUnitsPerTick { get; set; }
}
