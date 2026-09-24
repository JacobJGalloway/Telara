using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Moq;
using Telara.Mcp.Haiku.Clients;

namespace Telara.Mcp.Haiku.Tests;

// Telara.Mcp.Haiku's tools never touch Telara.Domain directly - their whole job is handing a call
// through McpClient to the Internal Functionality Server over HTTP (see InternalMcpClientProvider).
// Rather than standing up a real HTTP transport, this mocks McpClient itself at its one virtual,
// overridable seam - McpSession.SendRequestAsync(JsonRpcRequest, CancellationToken) - and wraps the
// canned McpClient in a provider whose GetClientAsync returns it without ever dialing out.
public static class TestFixture
{
    public static InternalMcpClientProvider CreateProvider(bool isError, string? content)
    {
        var mock = new Mock<McpClient>();
        mock.Setup(c => c.SendRequestAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonRpcRequest request, CancellationToken _) => new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToNode(new CallToolResult
                {
                    IsError = isError,
                    Content = content is null ? [] : [new TextContentBlock { Text = content }],
                }),
            });

        return new FakeInternalMcpClientProvider(mock.Object);
    }

    private sealed class FakeInternalMcpClientProvider(McpClient client)
        : InternalMcpClientProvider(Microsoft.Extensions.Options.Options.Create(new HaikuOptions { InternalServerUrl = "http://localhost/mcp" }))
    {
        public override Task<McpClient> GetClientAsync(CancellationToken cancellationToken) => Task.FromResult(client);
    }
}
