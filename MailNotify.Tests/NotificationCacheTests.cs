using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class NotificationCacheTests
{
    [Fact]
    public void Add_ReturnsTrueOnce_ThenDetectsDuplicateNotificationWithinSameCacheKind_WhenContentDidNotChange()
    {
        var cache = new NotificationCache(CreateSettingsProvider());
        var lastUpdate = DateTime.Today.AddHours(8);
        var notification = new Notification { Id = "1", Subject = "Daily", LastUpdate = lastUpdate };
        var duplicate = new Notification { Id = "1", Subject = "Daily", LastUpdate = lastUpdate.AddHours(1) };

        cache.Add(notification, NotificationCacheKind.Configured).Should().BeTrue();
        cache.Contains(duplicate, NotificationCacheKind.Configured).Should().BeTrue();
        cache.Add(duplicate, NotificationCacheKind.Configured).Should().BeFalse();
    }

    [Fact]
    public void Add_TreatsSameNotificationIdWithDifferentContentAsNewNotification()
    {
        var cache = new NotificationCache(CreateSettingsProvider());
        var notification = new Notification { Id = "1", Subject = "Original", Message = "Room 1" };
        var updatedNotification = new Notification { Id = "1", Subject = "Original", Message = "Room 2" };

        cache.Add(notification, NotificationCacheKind.Daily).Should().BeTrue();

        cache.Contains(updatedNotification, NotificationCacheKind.Daily).Should().BeFalse();
        cache.Add(updatedNotification, NotificationCacheKind.Daily).Should().BeTrue();
        cache.Contains(updatedNotification, NotificationCacheKind.Daily).Should().BeTrue();
    }

    [Fact]
    public void Add_KeepsDailyAndConfiguredCachesIndependent()
    {
        var cache = new NotificationCache(CreateSettingsProvider());
        var notification = new Notification { Id = "1", Subject = "Daily" };

        cache.Add(notification, NotificationCacheKind.Configured).Should().BeTrue();

        cache.Contains(notification, NotificationCacheKind.Configured).Should().BeTrue();
        cache.Contains(notification, NotificationCacheKind.Daily).Should().BeFalse();
        cache.Add(notification, NotificationCacheKind.Daily).Should().BeTrue();
    }

    [Fact]
    public void Remove_EvictsNotification()
    {
        var cache = new NotificationCache(CreateSettingsProvider());
        var notification = new Notification { Id = "1" };

        cache.Add(notification, NotificationCacheKind.Configured);
        cache.Remove(notification, NotificationCacheKind.Configured);

        cache.Contains(notification, NotificationCacheKind.Configured).Should().BeFalse();
    }

    private static ISettingsProvider CreateSettingsProvider()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.ReminderOffsetMinutes.Returns(15);
        return settingsProvider;
    }
}
