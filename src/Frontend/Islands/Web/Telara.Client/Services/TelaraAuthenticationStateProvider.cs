using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Telara.Client.Models;

namespace Telara.Client.Services;

public class TelaraAuthenticationStateProvider(GraphQlClient graphQlClient, AccessTokenAccessor tokenAccessor)
    : AuthenticationStateProvider
{
    // Refresh a bit ahead of actual expiry (access tokens live 15 min server-side) so a role
    // change (training completed, account terminated) shows up as soon as the user navigates
    // rather than only once the token has fully expired.
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(2);

    private const string LoginMutation = """
        mutation Login($email: String!, $password: String!) {
          login(input: { email: $email, password: $password }) {
            accessToken
            accessTokenExpiresAtUtc
            user { id email firstName lastName roleName }
          }
        }
        """;

    private const string RefreshTokenMutation = """
        mutation { refreshToken { accessToken accessTokenExpiresAtUtc user { id email firstName lastName roleName } } }
        """;

    private const string LogoutMutation = "mutation { logout }";

    private DateTime? _accessTokenExpiresAtUtc;
    private UserProfile? _currentUser;
    private bool _triedRestore;
    private bool _refreshing;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_triedRestore)
        {
            _triedRestore = true;
            await TryRestoreSessionAsync();
        }

        return new AuthenticationState(BuildPrincipal());
    }

    public async Task LoginAsync(string email, string password)
    {
        var response = await graphQlClient.SendAsync<LoginResponse>(LoginMutation, new { email, password });
        ApplyAuthPayload(response.Login);
    }

    public async Task LogoutAsync()
    {
        try
        {
            await graphQlClient.SendAsync<LogoutResponse>(LogoutMutation);
        }
        finally
        {
            ClearSession();
        }
    }

    // Fire-and-forget from App.razor on every route change - cheap when the token is still fresh
    // (no network call), and picks up a revoked/changed role within RefreshBuffer of navigating
    // rather than waiting out the rest of the token's lifetime.
    public async Task RefreshIfNeededAsync()
    {
        if (_refreshing || _currentUser is null)
            return;

        if (_accessTokenExpiresAtUtc is { } expires && expires - DateTime.UtcNow > RefreshBuffer)
            return;

        _refreshing = true;
        try
        {
            var response = await graphQlClient.SendAsync<RefreshTokenResponse>(RefreshTokenMutation);
            ApplyAuthPayload(response.RefreshToken);
        }
        catch (Exception ex) when (ex is GraphQlRequestException or HttpRequestException)
        {
            // Refresh cookie is gone/expired/revoked (e.g. an admin terminated the account), or
            // OpsApi is unreachable - either way, fall back to signed-out rather than leaving
            // GetAuthenticationStateAsync's Task faulted, which hangs AuthorizeRouteView on its
            // <Authorizing> template forever instead of resolving to signed-out.
            ClearSession();
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async Task TryRestoreSessionAsync()
    {
        try
        {
            var response = await graphQlClient.SendAsync<RefreshTokenResponse>(RefreshTokenMutation);
            ApplyAuthPayload(response.RefreshToken, notify: false);
        }
        catch (Exception ex) when (ex is GraphQlRequestException or HttpRequestException)
        {
            // No valid refresh cookie yet (first visit, it expired, or OpsApi is unreachable) -
            // stay signed out rather than leaving this Task faulted (see RefreshIfNeededAsync).
        }
    }

    private void ApplyAuthPayload(AuthPayload payload, bool notify = true)
    {
        tokenAccessor.AccessToken = payload.AccessToken;
        _accessTokenExpiresAtUtc = payload.AccessTokenExpiresAtUtc;
        _currentUser = payload.User;

        if (notify)
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal())));
    }

    private void ClearSession()
    {
        tokenAccessor.AccessToken = null;
        _accessTokenExpiresAtUtc = null;
        _currentUser = null;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal())));
    }

    private ClaimsPrincipal BuildPrincipal()
    {
        if (_currentUser is not { } user)
            return new ClaimsPrincipal(new ClaimsIdentity());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
        };

        if (user.RoleName is { Length: > 0 } role)
            claims.Add(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TelaraAuth"));
    }
}
