using Telara.Core.Generators.Interfaces;
using Telara.Domain.Telemetry;

namespace Telara.Core.Generators;

public class GeneratorInstanceFactory : IGeneratorInstanceFactory
{
    public GeneratorInstance Create(GeneratorInstanceConfig config)
    {
        var duplicateSensorId = config.Sensors
            .GroupBy(s => s.SensorId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateSensorId is not null)
            throw new InvalidOperationException(
                $"Generator config for equipment '{config.EquipmentId}' has duplicate SensorId '{duplicateSensorId.Key}'.");

        var key = new TelemetryKey(config.FacilityId, config.StationId, TelemetryLevel.Equipment, config.EquipmentId);
        return new GeneratorInstance(config, key);
    }
}
