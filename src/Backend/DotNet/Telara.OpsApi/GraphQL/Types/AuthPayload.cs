namespace Telara.OpsApi.GraphQL.Types;

public record AuthPayload(string AccessToken, DateTime AccessTokenExpiresAtUtc, UserProfile User);
