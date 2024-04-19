//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MultiFactor.Radius.Adapter.Core.Serialization
{
    public static class SerializerOptions
    {
        public static JsonSerializerOptions JsonSerializerOptions { get; }

        static SerializerOptions()
        {
            JsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }
    }
}
