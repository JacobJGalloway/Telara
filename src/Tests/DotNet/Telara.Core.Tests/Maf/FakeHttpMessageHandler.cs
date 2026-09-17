namespace Telara.Core.Tests.Maf;

// Same seam OpsApi.Tests uses to stand in for MAF's HTTP surface - lets MafClientTests assert on
// the request MafClient actually sent and control the response without a running MAF process.
public class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(responder(request));
}
