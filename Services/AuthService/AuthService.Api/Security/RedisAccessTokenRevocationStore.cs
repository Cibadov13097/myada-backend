using StackExchange.Redis;

namespace AuthService.API.Security;

public sealed class RedisAccessTokenRevocationStore : IAccessTokenRevocationStore
{
    private const string KeyPrefix = "auth:revoked:jti:";

    private readonly IDatabase _database;
    private readonly ILogger<RedisAccessTokenRevocationStore> _logger;

    public RedisAccessTokenRevocationStore(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisAccessTokenRevocationStore> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(jti))
            return;

        var ttl = expiresAtUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            _logger.LogDebug("Skipping token revocation for expired token JTI {Jti}.", jti);
            return;
        }

        try
        {
            await _database.StringSetAsync(
                BuildKey(jti),
                "1",
                ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write revoked token JTI {Jti} to Redis.", jti);
            throw;
        }
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(jti))
            return false;

        try
        {
            return await _database.KeyExistsAsync(BuildKey(jti));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read revoked token JTI {Jti} from Redis.", jti);
            return false;
        }
    }

    private static string BuildKey(string jti) => $"{KeyPrefix}{jti.Trim()}";
}
