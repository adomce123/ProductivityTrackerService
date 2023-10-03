using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductivityTrackerService.Configuration
{
    public static class SerializerConfiguration
    {
        public static JsonSerializerOptions DefaultSerializerOptions =>
            new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
    }
}
