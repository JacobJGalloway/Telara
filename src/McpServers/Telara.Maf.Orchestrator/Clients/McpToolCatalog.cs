namespace Telara.Maf.Orchestrator.Clients;

// Lazy tool discovery: the catalog is not fetched until the first caller actually needs it,
// and is cached afterward rather than re-querying tools/list on both servers on every call.
// This is the piece the routing rule in ARCHITECTURE.md's "MAF Orchestrator (This Sprint)"
// section will read from once /route/* dispatches by tool name instead of a hardcoded route.
public class McpToolCatalog(McpServerClientRegistry clients)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<DiscoveredTool>? _cache;

    public async Task<IReadOnlyList<DiscoveredTool>> GetToolsAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (_cache is not null && !forceRefresh)
            return _cache;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null && !forceRefresh)
                return _cache;

            var internalClient = await clients.GetInternalClientAsync(cancellationToken);
            var haikuClient = await clients.GetHaikuClientAsync(cancellationToken);

            var internalTools = await internalClient.ListToolsAsync(cancellationToken: cancellationToken);
            var haikuTools = await haikuClient.ListToolsAsync(cancellationToken: cancellationToken);

            _cache = internalTools
                .Select(t => new DiscoveredTool("internal", t.Name, t.Description))
                .Concat(haikuTools.Select(t => new DiscoveredTool("haiku", t.Name, t.Description)))
                .ToList();

            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }
}
