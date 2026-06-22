using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class DailyAppointmentServiceTests
{
    [Fact]
    public void GetDailyAppointments_ReturnsNull_WhenSettingIsDisabled()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        var notificationCache = Substitute.For<INotificationCache>();
        settingsProvider.NotifyDailyAppointments.Returns(false);
        var service = new DailyAppointmentService(settingsProvider, notificationCache);

        var notification = service.GetDailyAppointments([CreateNotification("future", DateTime.Now.AddMinutes(10))]);

        notification.Should().BeNull();
        notificationCache.DidNotReceiveWithAnyArgs().Contains(default!, default);
        notificationCache.DidNotReceiveWithAnyArgs().Add(default!, default);
    }

    [Fact]
    public void GetDailyAppointments_ReturnsOnlyFutureUncachedAppointments_AndCachesThem()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var past = CreateNotification("past", DateTime.Now.AddMinutes(-10));
        var cached = CreateNotification("cached", DateTime.Now.AddMinutes(10));
        var future = CreateNotification("future", DateTime.Now.AddMinutes(20));
        notificationCache.Contains(cached, NotificationCacheKind.Daily).Returns(true);
        var service = new DailyAppointmentService(settingsProvider, notificationCache);

        var notification = service.GetDailyAppointments([past, cached, future]);

        notification.Should().NotBeNull();
        notification!.Subject.Should().Be("New appointments for today");
        notification.Message.Should().Contain("future");
        notification.Message.Should().NotContain("past");
        notification.Message.Should().NotContain("cached");
        notificationCache.Received(1).Contains(cached, NotificationCacheKind.Daily);
        notificationCache.Received(1).Contains(future, NotificationCacheKind.Daily);
        notificationCache.DidNotReceive().Contains(past, NotificationCacheKind.Daily);
        notificationCache.Received(1).Add(future, NotificationCacheKind.Daily);
        notificationCache.DidNotReceive().Add(cached, NotificationCacheKind.Daily);
        notificationCache.DidNotReceive().Add(past, NotificationCacheKind.Daily);
    }

    [Fact]
    public void GetDailyAppointments_ReturnsNull_WhenAllFutureAppointmentsAreAlreadyCached()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var cached = CreateNotification("cached", DateTime.Now.AddMinutes(10));
        notificationCache.Contains(cached, NotificationCacheKind.Daily).Returns(true);
        var service = new DailyAppointmentService(settingsProvider, notificationCache);

        var notification = service.GetDailyAppointments([cached]);

        notification.Should().BeNull();
        notificationCache.DidNotReceive().Add(cached, NotificationCacheKind.Daily);
    }

    [Fact]
    public void GetDailyAppointments_LimitsMessageToFiveAppointments_AndShowsRemainderCount()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var appointments = Enumerable.Range(1, 7)
            .Select(i => CreateNotification($"meeting-{i}", DateTime.Now.AddMinutes(i)))
            .ToArray();
        var service = new DailyAppointmentService(settingsProvider, notificationCache);

        var notification = service.GetDailyAppointments(appointments);

        notification.Should().NotBeNull();
        notification!.Message.Should().Contain("meeting-1");
        notification.Message.Should().Contain("meeting-5");
        notification.Message.Should().NotContain("meeting-6");
        notification.Message.Should().NotContain("meeting-7");
        notification.Message.Should().Contain("and 2 more");
        appointments.ToList().ForEach(i => notificationCache.Received(1).Add(i, NotificationCacheKind.Daily));
    }

    private static ISettingsProvider CreateEnabledSettingsProvider()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.NotifyDailyAppointments.Returns(true);
        return settingsProvider;
    }

    private static ICalendarNotification CreateNotification(string id, DateTime start) =>
        new CalendarNotification
        {
            Id = id,
            Subject = id,
            Start = start,
            Duration = TimeSpan.FromMinutes(30)
        };
}
