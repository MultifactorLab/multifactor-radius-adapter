using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Infrastructure.Http
{
    public interface IHttpClientAdapter
    {
        Task<T> PostAsync<T>(string endpoint, object body, IReadOnlyDictionary<string, string> headers = null);
    }

    internal class HttpClientAdapter : IHttpClientAdapter
    {
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<HttpClientAdapter> _logger;

        public HttpClientAdapter(IHttpClientFactory factory, ILogger<HttpClientAdapter> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task<T> PostAsync<T>(string endpoint, object body, IReadOnlyDictionary<string, string> headers = null)
        {
            if (endpoint is null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = body == null ? null : CreateJsonStringContent(body)
            };

            AddHeaders(request, headers);

            var cli = _factory.CreateClient(nameof(HttpClientAdapter));
            _logger.LogDebug("Sending request to API: {@payload}", body);

            using var response = await cli.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var parsed = await DeserializeAsync<T>(response.Content);
            _logger.LogDebug("Received response from API: {@response}", parsed);

            return parsed;
        }

        private static void AddHeaders(HttpRequestMessage message, IReadOnlyDictionary<string, string> headers)
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
            var payload = JsonSerializer.Serialize(data, SerializerOptions.JsonSerializerOptions);
            return new StringContent(payload, Encoding.UTF8, "application/json");
        }

        private static async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            var jsonResponse = await content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions.JsonSerializerOptions);
            return parsed;
        }
    }
}
