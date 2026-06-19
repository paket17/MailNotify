using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class NotifyCacheTests
{
    [Fact]
    public void Add_ReturnsTrueOnce_ThenDetectsDuplicateNotification()
    {
        var cache = new NotifyCache(CreateSettingsProvider());
        var notification = new Notification { Id = "1", Subject = "Daily" };
        var duplicate = new Notification { Id = "1", Subject = "Daily" };

        cache.Add(notification).Should().BeTrue();
        cache.Contains(duplicate).Should().BeTrue();
        cache.Add(duplicate).Should().BeFalse();
    }

    [Fact]
    public void Remove_EvictsNotification()
    {
        var cache = new NotifyCache(CreateSettingsProvider());
        var notification = new Notification { Id = "1" };

        cache.Add(notification);
        cache.Remove(notification);

        cache.Contains(notification).Should().BeFalse();
    }

    private static ISettingsProvider CreateSettingsProvider()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.ReminderOffsetMinutes.Returns(15);
        return settingsProvider;
    }
}
