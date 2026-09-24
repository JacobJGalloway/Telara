using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Telara.Client.Services;

// Thin wrapper over raw POST /graphql, same request shape as the Postman walkthroughs in
// Telara.OpsApi/docs - no codegen client (StrawberryShake) yet, kept transparent while auth
// (bearer header, refresh-on-401) still needs to land on top of it.
//
// Auth is attached here directly, in-line, rather than via a DelegatingHandler
// (AddHttpMessageHandler<BearerTokenHandler>) - that approach silently never ran (confirmed via
// diagnostic logging: zero invocations across hundreds of requests), for reasons not pinned down
// in this WASM HttpClientFactory setup. Building the HttpRequestMessage explicitly here guarantees
// the header/credentials are set on every call, since it's inline in the code path that executes.
public class GraphQlClient(HttpClient httpClient, AccessTokenAccessor tokenAccessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TResponse> SendAsync<TResponse>(string query, object? variables = null, CancellationToken cancellationToken = default)
    {
        var body = new GraphQlRequest(query, variables);
        using var request = new HttpRequestMessage(HttpMethod.Post, "graphql")
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        if (tokenAccessor.AccessToken is { Length: > 0 } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var httpResponse = await httpClient.SendAsync(request, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<GraphQlResponse<TResponse>>(JsonOptions, cancellationToken)
            ?? throw new GraphQlRequestException("GraphQL response body was empty.");

        if (response.Errors is { Count: > 0 })
            throw new GraphQlRequestException(string.Join("; ", response.Errors.Select(e => e.Message)));

        return response.Data is null
            ? throw new GraphQlRequestException("GraphQL response had no data and no errors.")
            : response.Data;
    }

    private record GraphQlRequest(string Query, object? Variables);

    private record GraphQlResponse<TData>(TData? Data, List<GraphQlError>? Errors);

    private record GraphQlError(string Message, [property: JsonPropertyName("extensions")] Dictionary<string, JsonElement>? Extensions);
}

public sealed class GraphQlRequestException(string message) : Exception(message);
