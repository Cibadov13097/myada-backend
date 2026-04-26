using ClubManagementService.Application.ClubAdmin;
using ClubManagementService.Application.DTOs.Club;
using FluentAssertions;

namespace ClubManagementService.Tests.Layers.Application;

/// <summary>Large service — application layer guideline ~100–200+ tests (focused on pure helpers / DTO utilities).</summary>
public sealed class ClubManagementApplicationUtilitiesLayerTests
{
    [Theory]
    [InlineData(1, "job:1")]
    [InlineData(42, "job:42")]
    [InlineData(999, "job:999")]
    public void ClubAdminApplicationId_Job_formats_invariant(int id, string expected) =>
        ClubAdminApplicationId.Job(id).Should().Be(expected);

    [Fact]
    public void ClubAdminApplicationId_Membership_formats_guid_d()
    {
        var g = Guid.Parse("11111111-2222-3333-4444-555555555555");
        ClubAdminApplicationId.Membership(g).Should().Be("membership:11111111-2222-3333-4444-555555555555");
    }

    [Theory]
    [InlineData("job:5", 5)]
    [InlineData(" JOB:12 ", 12)]
    public void ClubAdminApplicationId_TryParse_job(string raw, int expectedJob)
    {
        var ok = ClubAdminApplicationId.TryParse(raw, out var jobId, out _);
        ok.Should().BeTrue();
        jobId.Should().Be(expectedJob);
    }

    [Fact]
    public void ClubAdminApplicationId_TryParse_membership()
    {
        var g = Guid.NewGuid();
        var ok = ClubAdminApplicationId.TryParse($"membership:{g:D}", out var jobId, out var membershipId);
        ok.Should().BeTrue();
        jobId.Should().Be(0);
        membershipId.Should().Be(g);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ClubAdminApplicationId_TryParse_whitespace_fails(string? raw)
    {
        var ok = ClubAdminApplicationId.TryParse(raw!, out _, out _);
        ok.Should().BeFalse();
    }

    [Theory]
    [InlineData("1", true, 1)]
    [InlineData(" 42 ", true, 42)]
    [InlineData("0", false, 0)]
    [InlineData("", false, 0)]
    public void ClubRouteId_TryParseClubKey(string raw, bool expected, int id)
    {
        var ok = ClubRouteId.TryParseClubKey(raw, out var numeric);
        ok.Should().Be(expected);
        numeric.Should().Be(id);
    }

    [Fact]
    public void ClubRouteId_TryParseClubKey_negative_returns_false_but_out_may_be_negative()
    {
        var ok = ClubRouteId.TryParseClubKey("-3", out var numeric);
        ok.Should().BeFalse();
        numeric.Should().Be(-3);
    }

    [Theory]
    [InlineData("https://example.com/path", true)]
    [InlineData("//cdn.example/x", true)]
    [InlineData("/relative", true)]
    [InlineData("mailto:test@example.com", true)]
    public void ClubUrlNormalizer_accepts_valid_inputs(string input, bool expectedOk)
    {
        var ok = ClubUrlNormalizer.TryNormalizeSocialOrResourceUrl(input, out _, out var err);
        ok.Should().Be(expectedOk);
        if (expectedOk)
            err.Should().BeNull();
    }

    [Theory]
    [InlineData("", false, "URL is empty.")]
    [InlineData("   ", false, "URL is empty.")]
    [InlineData("http://a b.com", false, "Invalid URL.")]
    public void ClubUrlNormalizer_rejects_invalid_inputs(string input, bool expectedOk, string partOfError)
    {
        var ok = ClubUrlNormalizer.TryNormalizeSocialOrResourceUrl(input, out _, out var err);
        ok.Should().Be(expectedOk);
        err.Should().Contain(partOfError);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    public void ClubContactEmail_whitespace_clears(string? input, bool okExpected)
    {
        var ok = ClubContactEmail.TryNormalize(input, out var normalized, out _);
        ok.Should().Be(okExpected);
        normalized.Should().BeNull();
    }

    [Theory]
    [InlineData("team@ada.edu.az", "team@ada.edu.az")]
    public void ClubContactEmail_valid_email_normalizes(string input, string expected)
    {
        var ok = ClubContactEmail.TryNormalize(input, out var normalized, out var err);
        ok.Should().BeTrue();
        err.Should().BeNull();
        normalized.Should().Be(expected);
    }

    [Fact]
    public void ClubContactEmail_invalid_returns_error()
    {
        var ok = ClubContactEmail.TryNormalize("not-an-email", out var normalized, out var err);
        ok.Should().BeFalse();
        normalized.Should().BeNull();
        err.Should().Contain("Invalid");
    }
}
