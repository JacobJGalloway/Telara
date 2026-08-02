namespace Telara.Core.Generators.Interfaces;

public interface IGeneratorInstanceFactory
{
    GeneratorInstance Create(GeneratorInstanceConfig config);
}
