using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ApiGateway.Tests.Layers.Gateway;

/// <summary>Gateway layer targets (small): ~0–5 routing / health / middleware checks.</summary>
public sealed class GatewayPresentationLayerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GatewayPresentationLayerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        _client = _factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Root_should_return_plain_text()
    {
        var response = await _client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("ApiGateway");
    }

    [Fact]
    public async Task Health_should_return_ok_payload()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<HealthPayload>();
        json.Should().NotBeNull();
        json!.status.Should().Be("healthy");
    }

    [Fact]
    public async Task Options_preflight_should_include_cors_headers_when_origin_allowed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/health")]
    public async Task Anonymous_endpoints_should_allow_get(string path)
    {
        var response = await _client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record HealthPayload(string status);
}
