namespace Telara.Core.Generators.Interfaces;

public interface ISensorValueGenerator
{
    decimal NextValue(SensorConfig sensor);
}
