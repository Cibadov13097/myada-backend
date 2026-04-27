using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NotificationService.Infrastructure.Messaging;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace NotificationService.Tests.Integration;

/// <summary>Notification list API with Postgres; Rabbit consumer removed so tests do not require RabbitMQ.</summary>
public sealed class NotificationListHttpIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();

        var dbName = "notif_it_" + Guid.NewGuid().ToString("N")[..10];
        await CreateDatabaseAsync(_postgres.GetConnectionString(), dbName);
        var cs = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = dbName }.ConnectionString;

        _factory = new NotificationIntegrationWebAppFactory(cs);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Notifications_list_flow_returns_ok()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/api/v1/notifications");
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

    private sealed class NotificationIntegrationWebAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public NotificationIntegrationWebAppFactory(string connectionString) => _connectionString = connectionString;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Develop", _connectionString);
            builder.ConfigureTestServices(services =>
            {
                foreach (var d in services.Where(d => d.ImplementationType == typeof(RabbitMqConsumer)).ToList())
                    services.Remove(d);
            });
        }
    }
}
