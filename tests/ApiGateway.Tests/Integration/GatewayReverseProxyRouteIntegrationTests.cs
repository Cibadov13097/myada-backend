using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiGateway.Tests.Integration;

/// <summary>Gateway routes forward to clusters; without running backends expect gateway errors (not success from origin).</summary>
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
    public async Task Proxied_route_without_cluster_should_fail_as_gateway_error(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        var response = await _client.SendAsync(request);

        response.IsSuccessStatusCode.Should().BeFalse(
            $"{path} should not return 2xx when downstream clusters are not reachable (run without full docker stack).");
    }
}
