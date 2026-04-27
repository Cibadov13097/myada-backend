using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace MyAda.Testing.Integration.Infrastructure;

/// <summary>Starts Postgres + Redis + AuthService in-process for JWKS-backed integration tests.</summary>
public sealed class AuthIntegrationServer : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private WebApplicationFactory<Program>? _factory;
    private string? _pemDirectory;

    /// <summary>Absolute JWKS URL (e.g. http://127.0.0.1:12345/.well-known/jwks.json).</summary>
    public string JwksUrl { get; private set; } = "";

    /// <summary>Redis connection string (shared container) for services that require Redis in integration tests.</summary>
    public string RedisConnectionString { get; private set; } = "";

    public HttpClient CreateAuthClient() => _factory!.CreateClient();

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();

        _redis = new RedisBuilder().Build();
        await _redis.StartAsync();

        var authDb = "auth_it_" + Guid.NewGuid().ToString("N")[..12];
        await CreateDatabaseAsync(_postgres.GetConnectionString(), authDb);

        var authCs = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = authDb
        }.ConnectionString;

        var redisCs = _redis.GetConnectionString();
        if (!redisCs.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
            redisCs += ",abortConnect=false";

        RedisConnectionString = redisCs;

        _pemDirectory = await ExtractAuthKeysToTempAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("ConnectionStrings:Develop", authCs);
            b.UseSetting("Redis:ConnectionString", redisCs);
            b.UseSetting("Jwt:PrivateKeyPath", Path.Combine(_pemDirectory, "private.pem"));
        });

        var client = _factory.CreateClient();
        JwksUrl = new Uri(client.BaseAddress!, ".well-known/jwks.json").AbsoluteUri;
    }

    /// <summary>Creates a new empty database on the shared Postgres instance and returns its connection string.</summary>
    public async Task<string> CreateDatabaseConnectionStringAsync(string namePrefix)
    {
        if (_postgres is null)
            throw new InvalidOperationException("Fixture not initialized.");

        var dbName = namePrefix + Guid.NewGuid().ToString("N")[..10];
        await CreateDatabaseAsync(_postgres.GetConnectionString(), dbName);
        return new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = dbName
        }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        if (_redis is not null)
            await _redis.DisposeAsync();

        if (_postgres is not null)
            await _postgres.DisposeAsync();

        if (_pemDirectory is not null && Directory.Exists(_pemDirectory))
        {
            try
            {
                Directory.Delete(_pemDirectory, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static async Task CreateDatabaseAsync(string adminConnectionString, string dbName)
    {
        if (dbName.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            throw new ArgumentException("Database name must be alphanumeric or underscore.", nameof(dbName));

        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand($"CREATE DATABASE {dbName}", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<string> ExtractAuthKeysToTempAsync()
    {
        var asm = typeof(AuthIntegrationServer).Assembly;
        var dir = Path.Combine(Path.GetTempPath(), "myada-auth-pem-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        await WriteEmbeddedAsync(asm, "MyAda.Testing.Integration.Infrastructure.AuthKeys.private.pem", Path.Combine(dir, "private.pem"));
        await WriteEmbeddedAsync(asm, "MyAda.Testing.Integration.Infrastructure.AuthKeys.public.pem", Path.Combine(dir, "public.pem"));
        return dir;
    }

    private static async Task WriteEmbeddedAsync(Assembly asm, string resourceName, string destPath)
    {
        await using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Missing embedded resource: " + resourceName);
        await using var fs = File.Create(destPath);
        await stream.CopyToAsync(fs);
    }
}
