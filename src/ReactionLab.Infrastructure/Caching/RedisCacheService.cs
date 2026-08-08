using Microsoft.Extensions.Logging;
using ReactionLab.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ReactionLab.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString(), SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET failed for key: {Key}", key);
            return default;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);

        if (cached is not null)
        {
            _logger.LogDebug("Cache HIT: {Key}", key);
            return cached;
        }

        _logger.LogDebug("Cache MISS: {Key}", key);

        var value = await factory();
        await SetAsync(key, value, expiration, cancellationToken);

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis DELETE failed for key: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} keys with prefix: {Prefix}", keys.Length, prefix);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis DELETE by prefix failed: {Prefix}", prefix);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, SerializerOptions);
            await _database.StringSetAsync(key, serialized, expiration ?? DefaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET failed for key: {Key}", key);
        }
    }
}
