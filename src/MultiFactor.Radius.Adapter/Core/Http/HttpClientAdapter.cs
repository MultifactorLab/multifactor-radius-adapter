using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using MultiFactor.Radius.Adapter.Core.Serialization;

namespace MultiFactor.Radius.Adapter.Core.Http
{
    public class HttpClientAdapter : IHttpClientAdapter
    {
        private readonly IHttpClientFactory _factory;
        private readonly IJsonDataSerializer _jsonDataSerializer;

        public HttpClientAdapter(IHttpClientFactory factory, IJsonDataSerializer jsonDataSerializer)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
        }

        public async Task<string> GetAsync(string uri, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            AddHeaders(message, headers);

            var resp = await ExecuteHttpMethod(() => GetClient().SendAsync(message));
            if (resp.Content == null) return default;

            return await resp.Content.ReadAsStringAsync();
        }

        public async Task<T> GetAsync<T>(string uri, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            AddHeaders(message, headers);

            var resp = await ExecuteHttpMethod(() => GetClient().SendAsync(message));
            if (resp.Content == null) return default;

            return await _jsonDataSerializer.DeserializeAsync<T>(resp.Content);
        }

        public async Task<T> PostAsync<T>(string uri, object data = null, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            AddHeaders(message, headers);
            if (data != null)
            {
                message.Content = _jsonDataSerializer.Serialize(data);
            }

            var resp = await ExecuteHttpMethod(() => GetClient().SendAsync(message));
            if (resp.Content == null) return default;

            return await _jsonDataSerializer.DeserializeAsync<T>(resp.Content);
        }

        public async Task<T> DeleteAsync<T>(string uri, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, uri);
            AddHeaders(message, headers);

            var resp = await ExecuteHttpMethod(() => GetClient().SendAsync(message));
            if (resp.Content == null) return default;

            return await _jsonDataSerializer.DeserializeAsync<T>(resp.Content);
        }

        private HttpClient GetClient() => _factory.CreateClient(nameof(HttpClientAdapter));

        private async Task<HttpResponseMessage> ExecuteHttpMethod(Func<Task<HttpResponseMessage>> method)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 100;

            // workaround for the .NET 4.6.2 version
            var response = await Task.Run(method);

            response.EnsureSuccessStatusCode();
            return response;
        }

        private static void AddHeaders(HttpRequestMessage message, IReadOnlyDictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var h in headers)
                {
                    message.Headers.Add(h.Key, h.Value);
                }
            }
        }
    }
}
