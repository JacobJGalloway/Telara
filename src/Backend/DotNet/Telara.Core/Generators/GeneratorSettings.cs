namespace Telara.Core.Generators;

public class GeneratorSettings
{
    public const string SectionName = "Generators";

    public List<GeneratorInstanceConfig> Instances { get; set; } = [];
}
