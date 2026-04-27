using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MyAda.Testing.Integration.Infrastructure;
using Xunit;

namespace AuthService.Tests.Integration;

/// <summary>HTTP integration flows: JWKS discovery, login, refresh (requires Docker for Testcontainers).</summary>
[Collection("AuthBackedIntegration")]
public sealed class AuthHttpFlowIntegrationTests
{
    private readonly AuthIntegrationServer _auth;

    public AuthHttpFlowIntegrationTests(AuthIntegrationServer auth) => _auth = auth;

    [Fact]
    public async Task Jwks_flow_returns_valid_document()
    {
        var client = _auth.CreateAuthClient();
        var response = await client.GetAsync(new Uri(".well-known/jwks.json", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("keys").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_flow_returns_access_and_refresh_tokens()
    {
        var client = _auth.CreateAuthClient();
        using var content = JsonContent.Create(new { username = "seed.user01", password = "SeedUser123!" });
        var response = await client.PostAsync("/api/auth/login", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_flow_returns_new_token_pair()
    {
        var client = _auth.CreateAuthClient();
        using var loginBody = JsonContent.Create(new { username = "seed.user01", password = "SeedUser123!" });
        var login = await client.PostAsync("/api/auth/login", loginBody);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginJson = await login.Content.ReadFromJsonAsync<JsonElement>();
        var refresh = loginJson.GetProperty("refreshToken").GetString();

        using var refreshBody = JsonContent.Create(new { refreshToken = refresh });
        var response = await client.PostAsync("/api/auth/refresh", refreshBody);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
