namespace Telara.OpsApi.GraphQL.Types;

public record UserProfile(long Id, string Email, string FirstName, string LastName, string? RoleName);
