using System.Text.Json;
using ClinicPOS.Application.Interfaces;
using StackExchange.Redis;

namespace ClinicPOS.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, ttl ?? TimeSpan.FromMinutes(5));
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct)
    {
        var endpoints = _redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            var keys = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            {
                keys.Add(key);
            }
            if (keys.Count > 0)
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(keys.ToArray());
            }
        }
    }
}
