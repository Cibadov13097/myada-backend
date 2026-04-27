using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MyAda.Testing.Integration.Infrastructure;
using Xunit;

namespace MyAdaSupportService.UnitTests.Integration;

/// <summary>Support categories API with JWT from Auth integration host.</summary>
[Collection("AuthBackedIntegration")]
public sealed class SupportCategoriesHttpIntegrationTests : IAsyncLifetime
{
    private readonly AuthIntegrationServer _auth;
    private WebApplicationFactory<MyAdaSupportService.Api.Program>? _factory;

    public SupportCategoriesHttpIntegrationTests(AuthIntegrationServer auth) => _auth = auth;

    public async Task InitializeAsync()
    {
        var cs = await _auth.CreateDatabaseConnectionStringAsync("support_it_");
        _factory = new WebApplicationFactory<MyAdaSupportService.Api.Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("ConnectionStrings:DefaultConnection", cs);
            b.UseSetting("Auth:JwksUrl", _auth.JwksUrl);
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
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
