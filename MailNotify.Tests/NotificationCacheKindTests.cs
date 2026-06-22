using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class NotificationCacheKindTests
{
    [Fact]
    public void Add_UsesNotificationIdAndCacheKindAsCacheKey()
    {
        var cache = new NotificationCache(CreateSettingsProvider());
        var notification = new Notification { Id = "1", Subject = "Original" };
        var changedNotification = new Notification { Id = "1", Subject = "Updated" };

        cache.Add(notification, NotificationCacheKind.Daily).Should().BeTrue();

        cache.Contains(changedNotification, NotificationCacheKind.Daily).Should().BeTrue();
        cache.Contains(changedNotification, NotificationCacheKind.Configured).Should().BeFalse();
        cache.Add(changedNotification, NotificationCacheKind.Daily).Should().BeFalse();
    }

    [Fact]
    public void Remove_EvictsNotificationOnlyFromRequestedCacheKind()
    {
        var cache = new NotificationCache(CreateSettingsProvider());
        var notification = new Notification { Id = "1", Subject = "Original" };

        cache.Add(notification, NotificationCacheKind.Daily);
        cache.Add(notification, NotificationCacheKind.Configured);
        cache.Remove(notification, NotificationCacheKind.Daily);

        cache.Contains(notification, NotificationCacheKind.Daily).Should().BeFalse();
        cache.Contains(notification, NotificationCacheKind.Configured).Should().BeTrue();
    }

    private static ISettingsProvider CreateSettingsProvider()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.ReminderOffsetMinutes.Returns(15);
        return settingsProvider;
    }
}
