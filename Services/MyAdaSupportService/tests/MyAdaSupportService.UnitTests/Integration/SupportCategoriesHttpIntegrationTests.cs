using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MyAda.Testing.Integration.Infrastructure;
using Xunit;

namespace MyAdaSupportService.UnitTests.Integration;

/// <summary>Support categories API with JWT from Auth integration host; Support API uses embedded Auth public key when IntegrationTests:UseEmbeddedJwtKey is true.</summary>
[Collection("AuthBackedIntegration")]
public sealed class SupportCategoriesHttpIntegrationTests : IAsyncLifetime
{
    private readonly AuthIntegrationServer _auth;
    private WebApplicationFactory<MyAdaSupportService.Api.Program>? _factory;
    private string? _prevMyAdaTestJwksUrlEnv;
    private string? _prevTestHostDisableHttpsEnv;
    private string? _prevConnectionStringEnv;

    public SupportCategoriesHttpIntegrationTests(AuthIntegrationServer auth) => _auth = auth;

    public async Task InitializeAsync()
    {
        var cs = await _auth.CreateDatabaseConnectionStringAsync("support_it_");
        _prevMyAdaTestJwksUrlEnv = Environment.GetEnvironmentVariable("MYADA_TEST_JWKS_URL");
        _prevTestHostDisableHttpsEnv = Environment.GetEnvironmentVariable("TestHost__DisableHttpsRedirection");
        _prevConnectionStringEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        Environment.SetEnvironmentVariable("MYADA_TEST_JWKS_URL", "1");
        Environment.SetEnvironmentVariable("TestHost__DisableHttpsRedirection", "true");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", cs);

        _factory = new WebApplicationFactory<MyAdaSupportService.Api.Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("IntegrationTests:UseEmbeddedJwtKey", "true");
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        SetOrClear("MYADA_TEST_JWKS_URL", _prevMyAdaTestJwksUrlEnv);
        SetOrClear("TestHost__DisableHttpsRedirection", _prevTestHostDisableHttpsEnv);
        SetOrClear("ConnectionStrings__DefaultConnection", _prevConnectionStringEnv);
    }

    private static void SetOrClear(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            Environment.SetEnvironmentVariable(name, null);
        else
            Environment.SetEnvironmentVariable(name, value);
    }

    [Fact]
    public async Task Categories_list_flow_returns_ok_with_bearer()
    {
        var authClient = _auth.CreateAuthClient();
        using var loginBody = JsonContent.Create(new { username = "seed.user01", password = "SeedUser123!" });
        var login = await authClient.PostAsync("/api/auth/login", loginBody);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        var client = _factory!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/Categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
