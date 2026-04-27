using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MyAdaAttendanceService.Web;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyAdaAttendanceService.Web.Tests.Integration;

/// <summary>Attendance admin courses list with Postgres (no auth middleware on the service).</summary>
public sealed class AttendanceAdminCoursesHttpIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();

        var dbName = "attend_it_" + Guid.NewGuid().ToString("N")[..10];
        await CreateDatabaseAsync(_postgres.GetConnectionString(), dbName);
        var cs = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = dbName }.ConnectionString;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("ConnectionStrings:DefaultConnection", cs);
            b.UseSetting("AuthService:SyncOnStartup", "false");
            b.UseSetting("AuthService:BaseUrl", "http://127.0.0.1:59997/");
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
    public async Task Admin_courses_list_flow_returns_ok()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/api/admin/courses");
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
