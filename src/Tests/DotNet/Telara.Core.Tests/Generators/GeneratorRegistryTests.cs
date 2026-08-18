using Telara.Core.Generators;
using Telara.Domain.Telemetry;

namespace Telara.Core.Tests.Generators;

public class GeneratorRegistryTests
{
    private static GeneratorInstance MakeInstance(string equipmentId, string instanceId)
    {
        var config = new GeneratorInstanceConfig
        {
            InstanceId = instanceId,
            FacilityId = "F1",
            StationId = "S1",
            EquipmentId = equipmentId,
        };
        var key = new TelemetryKey(config.FacilityId, config.StationId, TelemetryLevel.Equipment, equipmentId);

        return new GeneratorInstance(config, key);
    }

    [Fact]
    public void TryAdd_ReturnsTrue_ForNewKey()
    {
        var registry = new GeneratorRegistry();

        Assert.True(registry.TryAdd(MakeInstance("E1", "instance-1")));
        Assert.Single(registry.All);
    }

    [Fact]
    public void TryAdd_ReturnsFalse_ForDuplicateKey()
    {
        var registry = new GeneratorRegistry();
        registry.TryAdd(MakeInstance("E1", "instance-1"));

        var added = registry.TryAdd(MakeInstance("E1", "instance-2"));

        Assert.False(added);
        Assert.Single(registry.All);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenKeyNotPresent()
    {
        var registry = new GeneratorRegistry();
        var missingKey = new TelemetryKey("F1", "S1", TelemetryLevel.Equipment, "unknown");

        var found = registry.TryGet(missingKey, out var instance);

        Assert.False(found);
        Assert.Null(instance);
    }
}
