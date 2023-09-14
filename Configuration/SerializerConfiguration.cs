using System.Text.Json;

namespace ProductivityTrackerService.Configuration
{
    public static class SerializerConfiguration
    {
        public static JsonSerializerOptions DefaultSerializerOptions =>
            new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
    }
}
