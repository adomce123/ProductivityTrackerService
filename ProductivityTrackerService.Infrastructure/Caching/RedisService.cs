using Microsoft.Extensions.Configuration;
using ProductivityTrackerService.Core.Interfaces;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Caching
{
    public class RedisService : ISharedCache
    {
        private readonly IDatabase _database;

        public RedisService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Missing Redis connection string");

            var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            _database = connectionMultiplexer.GetDatabase();
        }

        public async Task SetAsync<T>(string key, T value)
        {
            var jsonString = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, jsonString);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var jsonString = await _database.StringGetAsync(key);
            return jsonString.HasValue ? JsonSerializer.Deserialize<T>(jsonString) : default;
        }
    }
}
