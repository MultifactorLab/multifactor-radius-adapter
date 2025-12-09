using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Http;

public interface IHttpClient
{
    Task<T?> PostAsync<T>(string endpoint, object? body, IReadOnlyDictionary<string, string>? headers = null);
}

public class MultifactorHttpClient : IHttpClient
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ILogger<MultifactorHttpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultifactorHttpClient(IHttpClientFactory factory, ILogger<MultifactorHttpClient> logger)
    {
        _client = factory.CreateClient(nameof(MultifactorHttpClient));
        _logger = logger;
        _jsonOptions = CreateJsonOptions();
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? body, IReadOnlyDictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        using var request = CreateRequest(endpoint, body, headers);
        
        _logger.LogDebug("Sending request to {Endpoint}: {@Body}", endpoint, body);
        
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await ParseResponse<T>(response);
        
        _logger.LogDebug("Received response from {Endpoint}: {@Response}", endpoint, result);
        
        return result;
    }

    private HttpRequestMessage CreateRequest(string endpoint, object? body, IReadOnlyDictionary<string, string>? headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        
        if (body != null)
            request.Content = CreateJsonContent(body);

        AddHeaders(request, headers);
        return request;
    }

    private static void AddHeaders(HttpRequestMessage request, IReadOnlyDictionary<string, string>? headers)
    {
        if (headers == null) 
            return;

        foreach (var header in headers)
            request.Headers.Add(header.Key, header.Value);
    }

    private HttpContent CreateJsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private async Task<T?> ParseResponse<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}