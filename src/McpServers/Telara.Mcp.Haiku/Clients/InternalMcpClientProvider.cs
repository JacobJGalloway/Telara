using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Telara.Mcp.Haiku.Clients;

// Haiku's one and only route to the data store: it never touches Telara.Domain or SQL Server
// directly, it calls back into the Internal Functionality Server's tools for both domain-model
// formatting and persistence. See "MAF Orchestrator (This Sprint)" in ARCHITECTURE.md.
public class InternalMcpClientProvider(IOptions<HaikuOptions> options)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private McpClient? _client;

    public async Task<McpClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
            return _client;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _client ??= await McpClient.CreateAsync(
                new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri(options.Value.InternalServerUrl) }),
                cancellationToken: cancellationToken);
            return _client;
        }
        finally
        {
            _gate.Release();
        }
    }
}
