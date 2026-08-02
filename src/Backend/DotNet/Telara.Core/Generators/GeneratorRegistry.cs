using System.Collections.Concurrent;
using Telara.Domain.Telemetry;

namespace Telara.Core.Generators;

public class GeneratorRegistry
{
    private readonly ConcurrentDictionary<TelemetryKey, GeneratorInstance> _instances = new();

    public bool TryAdd(GeneratorInstance instance) => _instances.TryAdd(instance.Key, instance);

    public bool TryGet(TelemetryKey key, out GeneratorInstance? instance) => _instances.TryGetValue(key, out instance);

    public IReadOnlyCollection<GeneratorInstance> All => _instances.Values.ToArray();
}
