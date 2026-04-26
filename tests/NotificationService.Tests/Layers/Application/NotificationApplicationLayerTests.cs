using FluentAssertions;
using Moq;
using NotificationService.Application.Interfaces;
using Xunit;

namespace NotificationService.Tests.Layers.Application;

/// <summary>Small service — application layer guideline ~10–25 tests.</summary>
public sealed class NotificationApplicationLayerTests
{
    private readonly Mock<IEmailSender> _email = new();

    [Fact]
    public async Task SendEmailAsync_forwards_to_email_sender_when_valid()
    {
        var svc = new global::NotificationService.Application.Services.NotificationService(_email.Object);

        await svc.SendEmailAsync("a@b.com", "subj", "body");

        _email.Verify(
            e => e.SendAsync("a@b.com", "subj", "body", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendEmailAsync_rejects_blank_email(string? email)
    {
        var svc = new global::NotificationService.Application.Services.NotificationService(_email.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.SendEmailAsync(email!, "s", "m"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendEmailAsync_rejects_blank_subject(string subject)
    {
        var svc = new global::NotificationService.Application.Services.NotificationService(_email.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.SendEmailAsync("a@b.com", subject, "m"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendEmailAsync_rejects_blank_message(string message)
    {
        var svc = new global::NotificationService.Application.Services.NotificationService(_email.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.SendEmailAsync("a@b.com", "s", message));
    }
}
