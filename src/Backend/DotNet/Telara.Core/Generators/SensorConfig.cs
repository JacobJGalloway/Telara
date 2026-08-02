namespace Telara.Core.Generators;

public class SensorConfig
{
    public string SensorId { get; set; } = default!;
    public string ReadingType { get; set; } = default!;
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
}
