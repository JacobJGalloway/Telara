using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Telara.Client.Services;

// Every GraphQL call needs BrowserRequestCredentials.Include, not just the auth ones - OpsApi is
// a different origin (port) from the Web Island, so the refresh cookie won't ride along on fetch
// requests otherwise. Bearer attachment is a no-op for anonymous calls (login itself, GetApiStatus).
public class BearerTokenHandler(AccessTokenAccessor tokenAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        if (tokenAccessor.AccessToken is { Length: > 0 } token)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
