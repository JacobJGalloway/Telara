using Mediator;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.CQRS.Queries;
using Telara.Domain.Entities;
using Telara.OpsApi.Auth;
using Telara.OpsApi.GraphQL.Types;

namespace Telara.OpsApi.GraphQL;

[MutationType]
public static partial class Mutation
{
    private const string RefreshCookieName = "telara_refresh_token";

    public static async Task<AuthPayload> Login(
        LoginInput input,
        [Service] ISender mediator,
        [Service] TokenService tokenService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var user = await mediator.Send(new AuthenticateUserQuery(input.Email, input.Password));
        if (user is null)
            throw new GraphQLException("Invalid email or password.");

        return await IssueTokensAsync(user, Guid.NewGuid(), mediator, tokenService, httpContextAccessor);
    }

    public static async Task<AuthPayload> RefreshToken(
        [Service] ISender mediator,
        [Service] TokenService tokenService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext!;
        var presentedToken = context.Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(presentedToken))
            throw new GraphQLException("Missing refresh token.");

        var result = await mediator.Send(new ValidateAndConsumeRefreshTokenCommand(TokenService.HashToken(presentedToken)));
        if (!result.Success)
        {
            context.Response.Cookies.Delete(RefreshCookieName);
            throw new GraphQLException("Refresh token is invalid or expired.");
        }

        var user = await mediator.Send(new GetUserByIdQuery(result.UserId!.Value));
        if (user is null)
        {
            context.Response.Cookies.Delete(RefreshCookieName);
            throw new GraphQLException("User no longer exists.");
        }

        return await IssueTokensAsync(user, result.FamilyId!.Value, mediator, tokenService, httpContextAccessor);
    }

    public static async Task<bool> Logout([Service] ISender mediator, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext!;
        var presentedToken = context.Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(presentedToken))
        {
            await mediator.Send(new RevokeRefreshTokenFamilyCommand(TokenService.HashToken(presentedToken)));
            context.Response.Cookies.Delete(RefreshCookieName);
        }

        return true;
    }

    private static async Task<AuthPayload> IssueTokensAsync(
        User user,
        Guid familyId,
        ISender mediator,
        TokenService tokenService,
        IHttpContextAccessor httpContextAccessor)
    {
        var (accessToken, accessTokenExpiresAtUtc) = tokenService.GenerateAccessToken(user);
        var refreshToken = TokenService.GenerateRefreshToken();
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(tokenService.RefreshTokenLifetimeDays);

        await mediator.Send(new CreateRefreshTokenCommand(
            user.Id,
            TokenService.HashToken(refreshToken),
            familyId,
            refreshTokenExpiresAtUtc));

        var context = httpContextAccessor.HttpContext!;
        context.Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshTokenExpiresAtUtc,
            Path = "/graphql",
        });

        return new AuthPayload(
            accessToken,
            accessTokenExpiresAtUtc,
            new UserProfile(user.Id, user.Email, user.FirstName, user.LastName, user.Role?.Name));
    }
}
