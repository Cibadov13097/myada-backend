using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace ClubManagementService.Tests.Integration;

/// <summary>Club management HTTP flows with Postgres and Development JWT bypass (no AuthService).</summary>
public sealed class ClubManagementHttpIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();

        var dbName = "club_it_" + Guid.NewGuid().ToString("N")[..10];
        await CreateDatabaseAsync(_postgres.GetConnectionString(), dbName);
        var cs = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = dbName }.ConnectionString;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("ConnectionStrings:Develop", cs);
            b.UseSetting("Auth:JwksUrl", string.Empty);
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Categories_list_flow_returns_ok()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/api/v1/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
