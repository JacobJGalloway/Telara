using System.Net;
using System.Net.Http.Json;
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Telara.Core.Maf;
using Telara.Domain.Entities;
using Telara.OpsApi.Auth;
using Telara.OpsApi.GraphQL.Types;

namespace Telara.OpsApi.Tests.GraphQL;

public class MutationTests
{
    private const string RefreshCookieName = "telara_refresh_token";
    private static readonly PasswordHasher<User> Hasher = new();

    private static MafClient CreateMafClient(bool isError, string content)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { isError, content = new[] { content } }),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new MafClient(httpClient);
    }

    private static TokenService CreateTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-signing-key-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "telara-tests",
                ["Jwt:Audience"] = "telara-tests",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14",
            })
            .Build();
        return new TokenService(configuration);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string? presentedRefreshToken = null)
    {
        var context = new DefaultHttpContext();
        if (presentedRefreshToken is not null)
            context.Request.Headers.Cookie = $"{RefreshCookieName}={presentedRefreshToken}";

        return new HttpContextAccessor { HttpContext = context };
    }

    private static User SeededUser(string email, string password)
    {
        var user = new User { Email = email, FirstName = "Ada", LastName = "Lovelace", AssignedStationId = "S1" };
        user.PasswordHash = Hasher.HashPassword(user, password);
        return user;
    }

    private static string? ExtractSetCookieValue(IHttpContextAccessor accessor) =>
        accessor.HttpContext!.Response.Headers.SetCookie
            .FirstOrDefault(h => h!.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));

    [Fact]
    public async Task RegisterStation_GreenPath_DeserializesResult()
    {
        var mafClient = CreateMafClient(isError: false,
            content: """{"facilityId":"F1","stationId":"S1","isLoadingDock":false,"predecessorStationIds":[]}""");

        var result = await Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", [], false, CancellationToken.None);

        Assert.Equal("S1", result.StationId);
    }

    [Fact]
    public async Task RegisterStation_MapsToConflict_WhenAlreadyRegistered()
    {
        var mafClient = CreateMafClient(isError: true, content: "Station S1 at facility F1 is already registered.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", [], false, CancellationToken.None));

        Assert.Equal("CONFLICT", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task RegisterStation_MapsToConflict_WhenPredecessorAlreadyLinked()
    {
        var mafClient = CreateMafClient(isError: true, content: "Station UPSTREAM at facility F1 cannot feed into a new successor - it already has one, or it is a loading dock.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", ["UPSTREAM"], false, CancellationToken.None));

        Assert.Equal("CONFLICT", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task RegisterStation_MapsToNotFound_WhenPredecessorMissing()
    {
        var mafClient = CreateMafClient(isError: true, content: "Station missing at facility F1 is not registered.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", ["missing"], false, CancellationToken.None));

        Assert.Equal("NOT_FOUND", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task RegisterStation_MapsToInternal_ForUnrecognizedFailure()
    {
        var mafClient = CreateMafClient(isError: true, content: "Something unexpected happened.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStation(mafClient, "F1", "S1", [], false, CancellationToken.None));

        Assert.Equal("INTERNAL", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task RegisterStationEquipment_GreenPath_DeserializesResult()
    {
        var mafClient = CreateMafClient(isError: false,
            content: """{"facilityId":"F1","stationId":"S1","equipmentId":"EQ-1","equipmentTypeId":6,"status":"Idle"}""");

        var result = await Telara.OpsApi.GraphQL.Mutation.RegisterStationEquipment(mafClient, "F1", "S1", "EQ-1", 6, CancellationToken.None);

        Assert.Equal("EQ-1", result.EquipmentId);
    }

    [Fact]
    public async Task RegisterStationEquipment_MapsToNotFound_WhenStationMissing()
    {
        var mafClient = CreateMafClient(isError: true, content: "Station S1 at facility F1 is not registered.");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RegisterStationEquipment(mafClient, "F1", "S1", "EQ-1", 6, CancellationToken.None));

        Assert.Equal("NOT_FOUND", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task Login_GreenPath_IssuesAccessToken_AndSetsRefreshCookie()
    {
        var (sender, db) = TestFixture.Create();
        db.Users.Add(SeededUser("ada@telara.dev", "correct-horse"));
        await db.SaveChangesAsync();
        var httpContextAccessor = CreateHttpContextAccessor();

        var payload = await Telara.OpsApi.GraphQL.Mutation.Login(
            new LoginInput("ada@telara.dev", "correct-horse"), sender, CreateTokenService(), httpContextAccessor);

        Assert.NotEmpty(payload.AccessToken);
        Assert.Equal("ada@telara.dev", payload.User.Email);
        Assert.NotNull(ExtractSetCookieValue(httpContextAccessor));
        Assert.Single(db.RefreshTokens);
    }

    [Fact]
    public async Task Login_Throws_WhenPasswordIsWrong()
    {
        var (sender, db) = TestFixture.Create();
        db.Users.Add(SeededUser("ada@telara.dev", "correct-horse"));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.Login(
                new LoginInput("ada@telara.dev", "wrong-password"), sender, CreateTokenService(), CreateHttpContextAccessor()));
    }

    [Fact]
    public async Task RefreshToken_GreenPath_RotatesToken_KeepingSameFamily()
    {
        var (sender, db) = TestFixture.Create();
        var user = SeededUser("ada@telara.dev", "correct-horse");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var tokenService = CreateTokenService();
        var loginAccessor = CreateHttpContextAccessor();

        await Telara.OpsApi.GraphQL.Mutation.Login(
            new LoginInput("ada@telara.dev", "correct-horse"), sender, tokenService, loginAccessor);
        var originalFamilyId = Assert.Single(db.RefreshTokens).FamilyId;
        var presentedToken = ExtractPresentedRefreshToken(loginAccessor);

        var refreshAccessor = CreateHttpContextAccessor(presentedToken);
        var refreshedPayload = await Telara.OpsApi.GraphQL.Mutation.RefreshToken(sender, tokenService, refreshAccessor);

        Assert.NotEmpty(refreshedPayload.AccessToken);
        Assert.NotNull(ExtractSetCookieValue(refreshAccessor));
        Assert.Equal(2, db.RefreshTokens.Count());
        Assert.All(db.RefreshTokens, t => Assert.Equal(originalFamilyId, t.FamilyId));
    }

    [Fact]
    public async Task RefreshToken_Throws_WhenCookieMissing()
    {
        var (sender, _) = TestFixture.Create();

        await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RefreshToken(sender, CreateTokenService(), CreateHttpContextAccessor()));
    }

    [Fact]
    public async Task RefreshToken_Throws_AndClearsCookie_WhenTokenUnknown()
    {
        var (sender, _) = TestFixture.Create();
        var accessor = CreateHttpContextAccessor("some-unknown-refresh-token");

        await Assert.ThrowsAsync<GraphQLException>(() =>
            Telara.OpsApi.GraphQL.Mutation.RefreshToken(sender, CreateTokenService(), accessor));

        Assert.Contains("expires=Thu, 01 Jan 1970", ExtractSetCookieValue(accessor), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_RevokesFamily_AndClearsCookie_WhenCookiePresent()
    {
        var (sender, db) = TestFixture.Create();
        var user = SeededUser("ada@telara.dev", "correct-horse");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var tokenService = CreateTokenService();
        var loginAccessor = CreateHttpContextAccessor();

        await Telara.OpsApi.GraphQL.Mutation.Login(
            new LoginInput("ada@telara.dev", "correct-horse"), sender, tokenService, loginAccessor);
        var presentedToken = ExtractPresentedRefreshToken(loginAccessor);
        var logoutAccessor = CreateHttpContextAccessor(presentedToken);

        var result = await Telara.OpsApi.GraphQL.Mutation.Logout(sender, logoutAccessor);

        Assert.True(result);
        Assert.NotNull(Assert.Single(db.RefreshTokens).RevokedAtUtc);
        Assert.Contains("expires=Thu, 01 Jan 1970", ExtractSetCookieValue(logoutAccessor), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_ReturnsTrue_WhenNoCookiePresent()
    {
        var (sender, _) = TestFixture.Create();

        var result = await Telara.OpsApi.GraphQL.Mutation.Logout(sender, CreateHttpContextAccessor());

        Assert.True(result);
    }

    // Login/RefreshToken only ever hand the raw refresh token to the browser via the Set-Cookie
    // header (the DB stores only its hash) - this walks the same path a real client would to
    // recover a presentable token for the next mutation in a chained test.
    private static string ExtractPresentedRefreshToken(IHttpContextAccessor accessor)
    {
        var setCookie = ExtractSetCookieValue(accessor)!;
        var start = setCookie.IndexOf('=') + 1;
        var end = setCookie.IndexOf(';', start);
        return setCookie[start..end];
    }
}
