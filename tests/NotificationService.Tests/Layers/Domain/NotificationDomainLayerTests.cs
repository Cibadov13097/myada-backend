using FluentAssertions;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Tests.Layers.Domain;

/// <summary>Small service — domain layer guideline ~5–15 tests.</summary>
public sealed class NotificationDomainLayerTests
{
    public static IEnumerable<object[]> Channels() =>
        Enum.GetValues<NotificationType>().Cast<NotificationType>().Select(v => new object[] { v });

    [Theory]
    [MemberData(nameof(Channels))]
    public void NotificationType_is_defined(NotificationType t) => Enum.IsDefined(t).Should().BeTrue();

    [Fact]
    public void Notification_entity_roundtrips_core_fields()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var n = new Notification
        {
            Id = id,
            Type = "alert",
            Message = "hello",
            CreatedAt = now,
            NotificationType = NotificationType.Email
        };
        n.Id.Should().Be(id);
        n.NotificationType.Should().Be(NotificationType.Email);
    }
}
