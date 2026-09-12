using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Telara.Client.Services;

// Thin wrapper over raw POST /graphql, same request shape as the Postman walkthroughs in
// Telara.OpsApi/docs - no codegen client (StrawberryShake) yet, kept transparent while auth
// (bearer header, refresh-on-401) still needs to land on top of it.
public class GraphQlClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TResponse> SendAsync<TResponse>(string query, object? variables = null, CancellationToken cancellationToken = default)
    {
        var request = new GraphQlRequest(query, variables);
        using var httpResponse = await httpClient.PostAsJsonAsync("graphql", request, JsonOptions, cancellationToken);
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
