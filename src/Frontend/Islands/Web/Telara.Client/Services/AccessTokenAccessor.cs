namespace Telara.Client.Services;

// Holds the in-memory access token so BearerTokenHandler can read it without depending on
// TelaraAuthenticationStateProvider (which itself depends on GraphQlClient - keeping the token
// here avoids a DI cycle between the two).
public class AccessTokenAccessor
{
    public string? AccessToken { get; set; }
}
