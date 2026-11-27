using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NotificationService.Api.Hubs;
using NotificationService.Api.Realtime;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Tests.Realtime;

public sealed class NotificationRealtimeNotifierTests
{
    [Fact]
    public async Task NotifyNotificationCreatedAsync_sends_event_to_recipient_user()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(proxy => proxy.SendCoreAsync(
                "notificationCreated",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>();
        hubClients
            .Setup(clients => clients.User("11111111-1111-1111-1111-111111111111"))
            .Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<NotificationHub>>();
        hubContext.SetupGet(context => context.Clients).Returns(hubClients.Object);

        var notifier = new NotificationRealtimeNotifier(hubContext.Object);
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Type = "club",
            Message = "New event alert",
            NotificationType = NotificationType.Push,
            RecipientUserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CreatedAt = DateTime.UtcNow
        };

        await notifier.NotifyNotificationCreatedAsync(notification, CancellationToken.None);

        hubClients.Verify(clients => clients.User("11111111-1111-1111-1111-111111111111"), Times.Once);
        clientProxy.Verify(
            proxy => proxy.SendCoreAsync(
                "notificationCreated",
                It.Is<object?[]>(args => HasPushChannelPayload(args)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNotificationCreatedAsync_skips_notifications_without_recipient()
    {
        var hubClients = new Mock<IHubClients>();
        var hubContext = new Mock<IHubContext<NotificationHub>>();
        hubContext.SetupGet(context => context.Clients).Returns(hubClients.Object);

        var notifier = new NotificationRealtimeNotifier(hubContext.Object);

        await notifier.NotifyNotificationCreatedAsync(new Notification
        {
            Id = Guid.NewGuid(),
            Type = "system",
            Message = "Broadcast-style seed data",
            NotificationType = NotificationType.Push,
            RecipientUserId = null,
            CreatedAt = DateTime.UtcNow
        }, CancellationToken.None);

        hubClients.Invocations.Should().BeEmpty();
    }

    private static bool HasPushChannelPayload(object?[] args)
    {
        if (args.Length != 1 || args[0] is null)
            return false;

        var payload = args[0]!;
        var channelProperty = payload.GetType().GetProperty("channel");
        var channelValue = channelProperty?.GetValue(payload)?.ToString();
        return string.Equals(channelValue, "Push", StringComparison.Ordinal);
    }
}
