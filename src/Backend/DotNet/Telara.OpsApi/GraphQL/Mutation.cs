using System.Text.Json;
using System.Text.Json.Serialization;
using Mediator;
using HotChocolate;
using HotChocolate.Authorization;
using Telara.Core.Maf;
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

    // The MCP SDK serializes enums (e.g. EquipmentStatus) as strings on the wire, using the
    // enum member's exact name (e.g. "Idle", not "idle") - plain JsonSerializerDefaults.Web
    // doesn't include a string-enum converter at all, so deserializing a tool's response
    // without one throws on any enum-bearing result type.
    private static readonly JsonSerializerOptions MafResultJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Station registration: routed through MAF to the Internal Functionality Server's tool
    // (register_station), per ARCHITECTURE.md's Definition of Done. isError text is translated
    // to a structured extensions.code the UI can act on - the GraphQL-native equivalent of the
    // "409 Conflict with a structured body" the doc calls for, since OpsApi has no REST surface
    // for registration to attach a literal HTTP status to.
    [Authorize]
    public static async Task<RegisterStationResult> RegisterStation(
        [Service] MafClient mafClient,
        string facilityId,
        string stationId,
        CancellationToken cancellationToken) =>
        await CallMafAndUnwrap<RegisterStationResult>(
            mafClient,
            read: true,
            toolName: "register_station",
            arguments: new Dictionary<string, object>
            {
                ["facilityId"] = facilityId,
                ["stationId"] = stationId,
            },
            cancellationToken);

    // Equipment registration: routed through MAF to the Claude Haiku Server's tool
    // (register_equipment), which itself calls back into the Internal Functionality Server -
    // same Haiku-drives-workflow/Internal-owns-persistence seam as the tools underneath it.
    [Authorize]
    public static async Task<RegisterStationEquipmentResult> RegisterStationEquipment(
        [Service] MafClient mafClient,
        string facilityId,
        string stationId,
        string equipmentId,
        int equipmentTypeId,
        CancellationToken cancellationToken) =>
        await CallMafAndUnwrap<RegisterStationEquipmentResult>(
            mafClient,
            read: false,
            toolName: "register_equipment",
            arguments: new Dictionary<string, object>
            {
                ["facilityId"] = facilityId,
                ["stationId"] = stationId,
                ["equipmentId"] = equipmentId,
                ["equipmentTypeId"] = equipmentTypeId,
            },
            cancellationToken);

    private static async Task<T> CallMafAndUnwrap<T>(
        MafClient mafClient, bool read, string toolName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var result = read
            ? await mafClient.CallReadAsync(toolName, arguments, cancellationToken)
            : await mafClient.CallOperationalAsync(toolName, arguments, cancellationToken);

        if (result.IsError)
        {
            var text = result.FirstText ?? $"{toolName} failed.";
            var code = text.Contains("already registered") || text.Contains("already exists")
                ? "CONFLICT"
                : text.Contains("is not registered")
                    ? "NOT_FOUND"
                    : "INTERNAL";

            throw new GraphQLException(ErrorBuilder.New().SetMessage(text).SetCode(code).Build());
        }

        return JsonSerializer.Deserialize<T>(result.FirstText!, MafResultJsonOptions)
            ?? throw new GraphQLException($"{toolName} returned an empty response.");
    }

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
