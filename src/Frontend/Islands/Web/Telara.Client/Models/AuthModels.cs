namespace Telara.Client.Models;

// Mirrors Telara.OpsApi.GraphQL.Types.AuthPayload/UserProfile - kept as separate client-side DTOs
// rather than a project reference, since an Island only talks to OpsApi over GraphQL, not by
// sharing backend assemblies.
public record AuthPayload(string AccessToken, DateTime AccessTokenExpiresAtUtc, UserProfile User);

public record UserProfile(long Id, string Email, string FirstName, string LastName, string? RoleName);

public record LoginResponse(AuthPayload Login);

public record RefreshTokenResponse(AuthPayload RefreshToken);

public record LogoutResponse(bool Logout);
