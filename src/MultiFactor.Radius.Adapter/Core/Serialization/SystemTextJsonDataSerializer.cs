using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MultiFactor.Radius.Adapter.Core.Serialization
{
    public class SystemTextJsonDataSerializer : IJsonDataSerializer
    {
        private readonly ILogger _logger;

        public SystemTextJsonDataSerializer(ILogger<SystemTextJsonDataSerializer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            var jsonResponse = await content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions.JsonSerializerOptions);
            _logger.LogDebug("Received response from API: {@response}", parsed);
            return parsed;
        }

        public StringContent Serialize(object data)
        {
            _logger.LogDebug("Sending request to API: {@payload}", data);
            var payload = JsonSerializer.Serialize(data, SerializerOptions.JsonSerializerOptions);
            return new StringContent(payload, Encoding.UTF8, "application/json");
        }
    }
}
