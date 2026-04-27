using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MyAda.Testing.JwksStub;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace CallService.Tests.Integration;

/// <summary>Call service health and ICE authorization with Postgres, Redis, and loopback JWKS.</summary>
public sealed class CallServiceHealthAndIceIntegrationTests : IAsyncLifetime
{
    private LoopbackJwksServer? _jwks;
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        _jwks = new LoopbackJwksServer();
        await _jwks.StartAsync();

        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();

        _redis = new RedisBuilder().Build();
        await _redis.StartAsync();

        var dbName = "call_it_" + Guid.NewGuid().ToString("N")[..10];
        await CreateDatabaseAsync(_postgres.GetConnectionString(), dbName);
        var cs = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = dbName }.ConnectionString;

        var redisCs = _redis.GetConnectionString();
        if (!redisCs.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
            redisCs += ",abortConnect=false";

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("Auth:JwksUrl", _jwks.JwksUrl);
            b.UseSetting("Redis:ConnectionString", redisCs);
            b.UseSetting("ConnectionStrings:CallService", cs);
            b.UseSetting("Turn:Enabled", "false");
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_redis is not null)
            await _redis.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
        if (_jwks is not null)
            await _jwks.DisposeAsync();
    }

    [Fact]
    public async Task Health_flow_returns_ok()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ice_servers_flow_requires_bearer()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/webrtc/ice-servers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task CreateDatabaseAsync(string adminConnectionString, string dbName)
    {
        if (dbName.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            throw new ArgumentException("Invalid database name.", nameof(dbName));

        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand($"CREATE DATABASE {dbName}", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
