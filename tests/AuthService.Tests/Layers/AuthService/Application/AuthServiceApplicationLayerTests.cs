using AuthService.Application.Options;
using FluentAssertions;

namespace AuthService.Tests.Layers.Application;

/// <summary>Medium service — application layer guideline ~30–80 tests (sample focused on options / configuration).</summary>
public sealed class AuthServiceApplicationLayerTests
{
    [Fact]
    public void JwtOptions_has_expected_section_name() =>
        JwtOptions.SectionName.Should().Be("Jwt");

    [Theory]
    [InlineData("issuer-a", "audience-a", 5, 7)]
    [InlineData("", "", 0, 0)]
    [InlineData("x", "y", 1440, 365)]
    public void JwtOptions_properties_roundtrip(string issuer, string audience, int accessMin, int refreshDays)
    {
        var o = new JwtOptions
        {
            Issuer = issuer,
            Audience = audience,
            AccessTokenLifetimeMinutes = accessMin,
            RefreshTokenLifetimeDays = refreshDays
        };
        o.Issuer.Should().Be(issuer);
        o.Audience.Should().Be(audience);
        o.AccessTokenLifetimeMinutes.Should().Be(accessMin);
        o.RefreshTokenLifetimeDays.Should().Be(refreshDays);
    }

    [Fact]
    public void JwtOptions_defaults_match_expected_baseline()
    {
        var o = new JwtOptions();
        o.Issuer.Should().Be("myada-auth");
        o.Audience.Should().Be("myada-api");
        o.AccessTokenLifetimeMinutes.Should().Be(15);
        o.RefreshTokenLifetimeDays.Should().Be(30);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(10080)]
    public void JwtOptions_access_token_lifetime_accepts_positive_minutes(int minutes)
    {
        new JwtOptions { AccessTokenLifetimeMinutes = minutes }.AccessTokenLifetimeMinutes.Should().Be(minutes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(400)]
    public void JwtOptions_refresh_token_lifetime_accepts_positive_days(int days)
    {
        new JwtOptions { RefreshTokenLifetimeDays = days }.RefreshTokenLifetimeDays.Should().Be(days);
    }
}
