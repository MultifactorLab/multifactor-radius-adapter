using System.Net.Http;
using System.Threading.Tasks;
using System;
using Serilog;
using System.Text;
using System.Text.Json;

namespace MultiFactor.Radius.Adapter.Core.Serialization
{
    public class SystemTextJsonDataSerializer : IJsonDataSerializer
    {
        private readonly ILogger _logger;

        public SystemTextJsonDataSerializer(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            var jsonResponse = await content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions.JsonSerializerOptions);
            _logger.Debug("Received response from API: {@response}", parsed);
            return parsed;
        }

        public StringContent Serialize(object data)
        {
            _logger.Debug("Sending request to API: {@payload}", data);
            var payload = JsonSerializer.Serialize(data, SerializerOptions.JsonSerializerOptions);
            return new StringContent(payload, Encoding.UTF8, "application/json");
        }
    }
}
