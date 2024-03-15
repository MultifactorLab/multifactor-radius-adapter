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

        public async Task<T> PostAsync<T>(string uri, object data = null, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            AddHeaders(message, headers);
            if (data != null)
            {
                message.Content = _jsonDataSerializer.Serialize(data);
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 100;

            var cli = _factory.CreateClient(nameof(HttpClientAdapter));
            var response = await cli.SendAsync(message);

            response.EnsureSuccessStatusCode();
            if (response.Content == null)
            {
                return default;
            }

            return await _jsonDataSerializer.DeserializeAsync<T>(response.Content);
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
