using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class DailyAppointmentServiceTests
{
    private static readonly DateTimeOffset Now = new(DateTime.Now);

    [Fact]
    public void GetDailyAppointments_ReturnsNull_WhenSettingIsDisabled()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        var notificationCache = Substitute.For<INotificationCache>();
        settingsProvider.NotifyDailyAppointments.Returns(false);
        var timeProvider = CreateTimeProvider(Now);
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        var notification = service.GetDailyAppointments([CreateNotification("future", Now.DateTime.AddMinutes(10))]);

        notification.Should().BeNull();
        notificationCache.DidNotReceiveWithAnyArgs().Contains(default!, default);
        notificationCache.DidNotReceiveWithAnyArgs().Add(default!, default);
    }

    [Fact]
    public void GetDailyAppointments_ReturnsOnlyFutureUncachedAppointments_AndCachesThem()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var timeProvider = CreateTimeProvider(Now);
        var past = CreateNotification("past", Now.DateTime.AddMinutes(-10));
        var cached = CreateNotification("cached", Now.DateTime.AddMinutes(10));
        var future = CreateNotification("future", Now.DateTime.AddMinutes(20));
        notificationCache.Contains(cached, NotificationCacheKind.Daily).Returns(true);
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        var notification = service.GetDailyAppointments([past, cached, future]);

        notification.Should().NotBeNull();
        notification!.Subject.Should().Be("Appointments for today");
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
        var timeProvider = CreateTimeProvider(Now);
        var cached = CreateNotification("cached", Now.DateTime.AddMinutes(10));
        notificationCache.Contains(cached, NotificationCacheKind.Daily).Returns(true);
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        var notification = service.GetDailyAppointments([cached]);

        notification.Should().BeNull();
        notificationCache.DidNotReceive().Add(cached, NotificationCacheKind.Daily);
    }

    [Fact]
    public void GetDailyAppointments_ShowsFirstThreeAppointmentsAndRemainderCountWithRemainingAppointments()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var timeProvider = CreateTimeProvider(Now);
        var appointments = Enumerable.Range(1, 7)
            .Select(i => CreateNotification($"meeting-{i}", Now.DateTime.AddMinutes(i)))
            .ToArray();
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        var notification = service.GetDailyAppointments(appointments);

        notification.Should().NotBeNull();
        var lines = notification!.Message!.Split(Environment.NewLine);
        lines.Should().Contain("and 4 more");
        var remainderIndex = Array.IndexOf(lines, "and 4 more");
        remainderIndex.Should().Be(3);
        lines.Take(3).Should().OnlyContain(l => l.Contains("meeting-"));
        lines.Skip(remainderIndex + 1).Should().Contain(l => l.Contains("meeting-4"));
        lines.Skip(remainderIndex + 1).Should().Contain(l => l.Contains("meeting-7"));
        appointments.ToList().ForEach(i => notificationCache.Received(1).Add(i, NotificationCacheKind.Daily));
    }

    [Fact]
    public void GetDailyAppointments_ShowsAllAppointmentsWithoutRemainder_WhenCountDoesNotExceedMax()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var timeProvider = CreateTimeProvider(Now);
        var appointments = Enumerable.Range(1, 4)
            .Select(i => CreateNotification($"meeting-{i}", Now.DateTime.AddMinutes(i)))
            .ToArray();
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        var notification = service.GetDailyAppointments(appointments);

        notification.Should().NotBeNull();
        notification!.Message.Should().Contain("meeting-1");
        notification.Message.Should().Contain("meeting-4");
        notification.Message.Should().NotContain("more");
    }

    [Fact]
    public void GetDailyAppointments_TruncatesLongSubjectsForVisibleAppointments_AndPreservesHidden()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var timeProvider = CreateTimeProvider(Now);
        var longSubject = new string('a', 60);
        var appointments = new[]
        {
            CreateNotification("1", Now.DateTime.AddMinutes(10), subject: longSubject),
            CreateNotification("2", Now.DateTime.AddMinutes(20), subject: "Short"),
            CreateNotification("3", Now.DateTime.AddMinutes(30), subject: "Another"),
            CreateNotification("4", Now.DateTime.AddMinutes(40), subject: "Another 2"),
            CreateNotification("5", Now.DateTime.AddMinutes(50), subject: "Hidden " + longSubject)
        };
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        var notification = service.GetDailyAppointments(appointments);

        notification.Should().NotBeNull();
        var lines = notification!.Message!.Split(Environment.NewLine);
        lines[0].Should().EndWith("…");
        lines[0].Length.Should().Be(41); // "HH:mm " (6) + 34 chars + "…" (1) = 41
        lines[5].Should().Contain("Hidden " + longSubject); // hidden meeting preserved without truncation
    }

    [Fact]
    public void GetDailyAppointments_ReturnsFirstDailySubject_WhenNoNotificationWasSentToday()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var timeProvider = CreateTimeProvider(Now);
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        var notification = service.GetDailyAppointments([CreateNotification("future", Now.DateTime.AddMinutes(10))]);

        notification.Should().NotBeNull();
        notification!.Subject.Should().Be("Appointments for today");
    }

    [Fact]
    public void GetDailyAppointments_ReturnsNewDailySubject_WhenNotificationWasAlreadySentToday()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var timeProvider = CreateTimeProvider(Now);
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        service.GetDailyAppointments([CreateNotification("first", Now.DateTime.AddMinutes(10))]);
        var notification = service.GetDailyAppointments([CreateNotification("second", Now.DateTime.AddMinutes(20))]);

        notification.Should().NotBeNull();
        notification!.Subject.Should().Be("New appointments for today");
    }

    [Fact]
    public void GetDailyAppointments_ReturnsFirstDailySubject_WhenLastNotificationWasSentBeforeToday()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = Substitute.For<INotificationCache>();
        var currentTime = Now.AddDays(-1);
        var timeProvider = CreateTimeProvider(() => currentTime);
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);

        service.GetDailyAppointments([CreateNotification("yesterday", currentTime.DateTime.AddMinutes(10))]);
        currentTime = Now;
        var notification = service.GetDailyAppointments([CreateNotification("future", currentTime.DateTime.AddMinutes(10))]);

        notification.Should().NotBeNull();
        notification!.Subject.Should().Be("Appointments for today");
    }

    [Fact]
    public void GetDailyAppointments_ReturnsUpdatedAppointment_WhenContentChanged()
    {
        var settingsProvider = CreateEnabledSettingsProvider();
        var notificationCache = new NotificationCache(
            CreateCacheSettingsProvider(),
            new NotificationHashBuilderResolver(new NotificationHashBuilder(), new CalendarNotificationHashBuilder()));
        var timeProvider = CreateTimeProvider(Now);
        var state = new DailyAppointmentNotificationState();
        var service = new DailyAppointmentService(settingsProvider, notificationCache, timeProvider, state);
        var appointment = CreateNotification("meeting", Now.DateTime.AddMinutes(10), subject: "meeting");
        var updatedAppointment = CreateNotification("meeting", Now.DateTime.AddMinutes(10), subject: "meeting updated");

        service.GetDailyAppointments([appointment]);
        var notification = service.GetDailyAppointments([updatedAppointment]);

        notification.Should().NotBeNull();
        notification!.Subject.Should().Be("New appointments for today");
        notification.Message.Should().Contain("meeting updated");
    }

    private static ISettingsProvider CreateEnabledSettingsProvider()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.NotifyDailyAppointments.Returns(true);
        return settingsProvider;
    }

    private static ISettingsProvider CreateCacheSettingsProvider()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.ReminderOffsetMinutes.Returns(15);
        return settingsProvider;
    }

    private static ICalendarNotification CreateNotification(string id, DateTime start, DateTime? lastUpdate = null, string? subject = null) =>
        new CalendarNotification
        {
            Id = id,
            Subject = subject ?? id,
            Start = start,
            Duration = TimeSpan.FromMinutes(30),
            LastUpdate = lastUpdate ?? default
        };

    private static TimeProvider CreateTimeProvider(DateTimeOffset now) =>
        CreateTimeProvider(() => now);

    private static TimeProvider CreateTimeProvider(Func<DateTimeOffset> now)
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
        timeProvider.GetUtcNow().Returns(_ => now());
        return timeProvider;
    }
}
