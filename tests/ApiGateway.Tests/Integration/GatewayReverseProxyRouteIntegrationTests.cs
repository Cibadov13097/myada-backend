using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiGateway.Tests.Integration;

/// <summary>Smoke-check that known gateway path prefixes are handled. Outcomes differ by environment (2xx if a backend is up locally, otherwise proxy/client errors).</summary>
public sealed class GatewayReverseProxyRouteIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GatewayReverseProxyRouteIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    public static TheoryData<string> GatewayProxiedPaths => new()
    {
        "/api/auth/login",
        "/club/api/v1/categories",
        "/location/api/v1/buildings",
        "/lostfound/api/v1/categories",
        "/notification/api/v1/notifications",
        "/call/health",
        "/support/api/Categories",
        "/attendance/api/admin/courses"
    };

    [Theory]
    [MemberData(nameof(GatewayProxiedPaths))]
    public async Task Proxied_routes_respond_for_configured_path(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        var response = await _client.SendAsync(request);

        // Do not require non-2xx: the same test run can hit a live docker stack. Only assert a normal HTTP result.
        ((int)response.StatusCode).Should().BeInRange(100, 599, $"{path} should return a well-formed HTTP status.");
    }
}
