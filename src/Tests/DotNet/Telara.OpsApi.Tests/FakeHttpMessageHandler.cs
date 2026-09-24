namespace Telara.OpsApi.Tests;

// Stands in for MAF's HTTP surface so Mutation/Query tests can exercise MafClient's real
// serialization and Mutation's error-code mapping without a running MAF process - the responder
// gets the raw request so a test can assert on the tool name/arguments MafClient actually sent.
public class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(responder(request));
}
