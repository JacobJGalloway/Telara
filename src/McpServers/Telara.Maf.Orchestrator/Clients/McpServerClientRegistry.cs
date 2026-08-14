using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Telara.Maf.Orchestrator.Clients;

// MAF's connection to the two MCP servers it routes between. One MCP client per server,
// created lazily and reused - not one per request. See "MAF Orchestrator (This Sprint)"
// in ARCHITECTURE.md: MAF itself is a stateless router, but the underlying MCP client
// connections are still worth holding open rather than reconnecting on every call.
public class McpServerClientRegistry(IOptions<MafOptions> options)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private McpClient? _internalClient;
    private McpClient? _haikuClient;

    public Task<McpClient> GetInternalClientAsync(CancellationToken cancellationToken) =>
        GetOrCreateAsync(() => _internalClient, c => _internalClient = c, options.Value.InternalServerUrl, cancellationToken);

    public Task<McpClient> GetHaikuClientAsync(CancellationToken cancellationToken) =>
        GetOrCreateAsync(() => _haikuClient, c => _haikuClient = c, options.Value.HaikuServerUrl, cancellationToken);

    private async Task<McpClient> GetOrCreateAsync(
        Func<McpClient?> get, Action<McpClient> set, string url, CancellationToken cancellationToken)
    {
        var existing = get();
        if (existing is not null)
            return existing;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            existing = get();
            if (existing is not null)
                return existing;

            var client = await McpClient.CreateAsync(
                new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri(url) }),
                cancellationToken: cancellationToken);
            set(client);
            return client;
        }
        finally
        {
            _gate.Release();
        }
    }
}
