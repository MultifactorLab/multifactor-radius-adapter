using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Http;

public interface IHttpClient
{
    Task<T?> PostAsync<T>(string endpoint, object body, IReadOnlyDictionary<string, string>? headers = null);
}

public class MultifactorHttpClient : IHttpClient
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<MultifactorHttpClient> _logger;
    
    public MultifactorHttpClient(IHttpClientFactory factory, ILogger<MultifactorHttpClient> logger)
    {
        _factory = factory;
        _logger = logger;
    }
    
    public async Task<T?> PostAsync<T>(string endpoint, object? body, IReadOnlyDictionary<string, string>? headers = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = body == null ? null : CreateJsonStringContent(body)
        };

        AddHeaders(request, headers);

        var cli = _factory.CreateClient(nameof(MultifactorHttpClient));
        _logger.LogDebug("Sending request to API: {@payload}", body);

        using var response = await cli.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var parsed = await DeserializeAsync<T>(response.Content);
        _logger.LogDebug("Received response from API: {@response}", parsed);

        return parsed;
    }
    
    private static void AddHeaders(HttpRequestMessage message, IReadOnlyDictionary<string, string>? headers)
    {
        if (headers == null)
        {
            return;
        }

        foreach (var h in headers)
        {
            message.Headers.Add(h.Key, h.Value);
        }
    }

    private static StringContent CreateJsonStringContent(object data)
    {
        var payload = JsonSerializer.Serialize(data, GetJsonSerializerOptions());
        return new StringContent(payload, Encoding.UTF8, "application/json");
    }

    private static async Task<T?> DeserializeAsync<T>(HttpContent content)
    {
        var jsonResponse = await content.ReadAsStringAsync();
        var parsed = JsonSerializer.Deserialize<T>(jsonResponse, GetJsonSerializerOptions());
        return parsed;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}