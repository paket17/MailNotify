using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class NotificationCacheTests
{
    [Fact]
    public void Add_ReturnsTrueOnce_ThenDetectsDuplicateNotificationWithinSameCacheKind_WhenContentDidNotChange()
    {
        var cache = CreateCache();
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
        var cache = CreateCache();
        var notification = new Notification { Id = "1", Subject = "Original", Message = "Room 1" };
        var updatedNotification = new Notification { Id = "1", Subject = "Original", Message = "Room 2" };

        cache.Add(notification, NotificationCacheKind.Daily).Should().BeTrue();

        cache.Contains(updatedNotification, NotificationCacheKind.Daily).Should().BeFalse();
        cache.Add(updatedNotification, NotificationCacheKind.Daily).Should().BeTrue();
        cache.Contains(updatedNotification, NotificationCacheKind.Daily).Should().BeTrue();
    }

    [Fact]
    public void Add_TreatsSameCalendarNotificationIdWithDifferentStartEndOrLocationAsNewNotification()
    {
        var cache = CreateCache();
        var start = DateTime.Today.AddHours(10);
        var notification = new CalendarNotification
        {
            Id = "1",
            Subject = "Daily",
            Start = start,
            Duration = TimeSpan.FromMinutes(30),
            Location = "Room 1"
        };
        var changedStart = notification with { Start = start.AddMinutes(15) };
        var changedEnd = notification with { Duration = TimeSpan.FromMinutes(45) };
        var changedLocation = notification with { Location = "Room 2" };

        cache.Add(notification, NotificationCacheKind.Daily).Should().BeTrue();

        cache.Contains(changedStart, NotificationCacheKind.Daily).Should().BeFalse();
        cache.Contains(changedEnd, NotificationCacheKind.Daily).Should().BeFalse();
        cache.Contains(changedLocation, NotificationCacheKind.Daily).Should().BeFalse();
    }

    [Fact]
    public void Add_KeepsDailyAndConfiguredCachesIndependent()
    {
        var cache = CreateCache();
        var notification = new Notification { Id = "1", Subject = "Daily" };

        cache.Add(notification, NotificationCacheKind.Configured).Should().BeTrue();

        cache.Contains(notification, NotificationCacheKind.Configured).Should().BeTrue();
        cache.Contains(notification, NotificationCacheKind.Daily).Should().BeFalse();
        cache.Add(notification, NotificationCacheKind.Daily).Should().BeTrue();
    }

    [Fact]
    public void Remove_EvictsNotification()
    {
        var cache = CreateCache();
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

    private static NotificationCache CreateCache() =>
        new(CreateSettingsProvider(), CreateHashBuilderResolver());

    private static INotificationHashBuilderResolver CreateHashBuilderResolver() =>
        new NotificationHashBuilderResolver(new NotificationHashBuilder(), new CalendarNotificationHashBuilder());
}
