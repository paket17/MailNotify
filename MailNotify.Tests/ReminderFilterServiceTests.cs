using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class ReminderFilterServiceTests
{
    [Fact]
    public void GetReminders_ReturnsOnlyUpcomingUncachedNotificationsWithinReminderWindow()
    {
        var cache = Substitute.For<INotifyCache>();
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.ReminderOffsetMinutes.Returns(10);
        var service = new ReminderFilterService(cache, settingsProvider);
        var now = DateTime.Now;
        var due = CreateNotification("due", now.AddMinutes(2));
        var future = CreateNotification("future", now.AddMinutes(30));
        var started = CreateNotification("started", now.AddMinutes(-1));
        var cached = CreateNotification("cached", now.AddMinutes(2));
        cache.Contains(Arg.Is<ICalendarNotification>(i => i.Id == "cached")).Returns(true);

        var reminders = service.GetReminders([due, future, started, cached]).ToArray();

        reminders.Select(i => i.Id!).Should().Equal("due");
        cache.Received(1).Contains(due);
        cache.Received(1).Contains(cached);
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
