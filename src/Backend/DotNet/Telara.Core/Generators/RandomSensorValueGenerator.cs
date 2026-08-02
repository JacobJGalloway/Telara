using Telara.Core.Generators.Interfaces;

namespace Telara.Core.Generators;

public class RandomSensorValueGenerator : ISensorValueGenerator
{
    public decimal NextValue(SensorConfig sensor)
    {
        var range = (double)(sensor.MaxValue - sensor.MinValue);
        var value = (double)sensor.MinValue + (Random.Shared.NextDouble() * range);
        return Math.Round((decimal)value, 4);
    }
}
