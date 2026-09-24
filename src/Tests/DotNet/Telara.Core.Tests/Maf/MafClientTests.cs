using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Telara.Core.Maf;

namespace Telara.Core.Tests.Maf;

public class MafClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static MafClient CreateClient(
        out Func<HttpRequestMessage> capturedRequest,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        object? responseBody = null)
    {
        HttpRequestMessage? request = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            request = req;
            return new HttpResponseMessage(statusCode)
            {
                // A literal JSON "null" body, not an empty one, is what actually deserializes to a
                // null MafRouteResult and exercises MafClient's fallback-error path.
                Content = responseBody is null
                    ? new StringContent("null", System.Text.Encoding.UTF8, "application/json")
                    : JsonContent.Create(responseBody, options: JsonOptions),
            };
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        capturedRequest = () => request ?? throw new InvalidOperationException("No request was captured.");
        return new MafClient(httpClient);
    }

    [Fact]
    public async Task CallOperationalAsync_PostsToOperationalRoute_WithToolNameAndArguments()
    {
        var client = CreateClient(out var capturedRequest,
            responseBody: new { isError = false, content = new[] { "ok" } });

        await client.CallOperationalAsync("RegisterStation", new Dictionary<string, object> { ["stationId"] = "S1" }, CancellationToken.None);

        var request = capturedRequest();
        Assert.Equal("/route/operational", request.RequestUri!.AbsolutePath);
        var body = await request.Content!.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("RegisterStation", body.GetProperty("toolName").GetString());
        Assert.Equal("S1", body.GetProperty("arguments").GetProperty("stationId").GetString());
    }

    [Fact]
    public async Task CallReadAsync_PostsToReadRoute()
    {
        var client = CreateClient(out var capturedRequest,
            responseBody: new { isError = false, content = new[] { "ok" } });

        await client.CallReadAsync("GetStationWorkflow", new Dictionary<string, object>(), CancellationToken.None);

        Assert.Equal("/route/read", capturedRequest().RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CallAsync_ReturnsDeserializedResult_OnSuccess()
    {
        var client = CreateClient(out _,
            responseBody: new { isError = false, content = new[] { "S1", "S2" } });

        var result = await client.CallReadAsync("GetStationWorkflow", new Dictionary<string, object>(), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(["S1", "S2"], result.Content);
        Assert.Equal("S1", result.FirstText);
    }

    [Fact]
    public async Task CallAsync_ReturnsErrorResult_WithFirstTextNull_WhenContentEmpty()
    {
        var client = CreateClient(out _,
            responseBody: new { isError = true, content = Array.Empty<string>() });

        var result = await client.CallReadAsync("GetStationWorkflow", new Dictionary<string, object>(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Null(result.FirstText);
    }

    [Fact]
    public async Task CallAsync_ReturnsFallbackErrorResult_WhenResponseBodyIsNull()
    {
        var client = CreateClient(out _, responseBody: null);

        var result = await client.CallReadAsync("GetStationWorkflow", new Dictionary<string, object>(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("GetStationWorkflow", result.FirstText);
    }

    [Fact]
    public async Task CallAsync_Throws_WhenMafReturnsNonSuccessStatusCode()
    {
        var client = CreateClient(out _, statusCode: HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CallReadAsync("GetStationWorkflow", new Dictionary<string, object>(), CancellationToken.None));
    }
}
