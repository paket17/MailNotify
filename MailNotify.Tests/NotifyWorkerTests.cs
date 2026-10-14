using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;
using Microsoft.Extensions.Logging;

namespace MailNotify.Tests;

public class NotifyWorkerTests
{
    [Fact]
    public void GetAppointments_UpdatesStoreAndSendsDailyAppointments_WhenNotificationsFound()
    {
        var source = new[]
        {
            CreateNotification("source"),
            CreateNotification("second")
        };
        var dailyAppointments = CreateNotification("daily-summary");
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var dailyService = Substitute.For<IDailyAppointmentService>();
        var store = new AppointmentStore();
        var start = DateTime.Today;
        var end = start.AddDays(1).AddSeconds(-1);
        getter.GetNotifications(start, end).Returns(source);
        dailyService.GetDailyAppointments(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(source)))
            .Returns(dailyAppointments);
        var worker = CreateWorker(getter, sender, filter, dailyService, store);

        worker.GetAppointments(CancellationToken.None);

        getter.Received(1).GetNotifications(start, end);
        store.GetAppointments().Should().Equal(source);
        dailyService.Received(1)
            .GetDailyAppointments(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(source)));
        sender.Received(1).SendNotification(dailyAppointments, false);
        filter.DidNotReceiveWithAnyArgs().GetReminders(default!);
    }

    [Fact]
    public void GetAppointments_ReturnsWithoutSending_WhenGettingNotificationsFails()
    {
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var dailyService = Substitute.For<IDailyAppointmentService>();
        var store = new AppointmentStore();
        getter.GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(_ => throw new InvalidOperationException("Exchange failed"));
        var worker = CreateWorker(getter, sender, filter, dailyService, store);

        worker.GetAppointments(CancellationToken.None);

        dailyService.DidNotReceiveWithAnyArgs().GetDailyAppointments(default!);
        sender.DidNotReceiveWithAnyArgs().SendNotification(default!, default);
        store.GetAppointments().Should().BeEmpty();
    }

    [Fact]
    public void GetAppointments_ContinuesSending_WhenOneNotificationFails()
    {
        var first = CreateNotification("first");
        var second = CreateNotification("second");
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var dailyService = Substitute.For<IDailyAppointmentService>();
        getter.GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([first, second]);
        dailyService.GetDailyAppointments(Arg.Any<IEnumerable<ICalendarNotification>>()).Returns(first);
        sender.When(i => i.SendNotification(first, false)).Do(_ => throw new InvalidOperationException("Toast failed"));
        var worker = CreateWorker(getter, sender, filter, dailyService);

        worker.GetAppointments(CancellationToken.None);

        sender.Received(1).SendNotification(first, false);
    }

    [Fact]
    public void GetAppointments_DoesNotSend_WhenCancellationIsAlreadyRequested()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var dailyService = Substitute.For<IDailyAppointmentService>();
        var summary = CreateNotification("summary");
        getter.GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([CreateNotification("m1")]);
        dailyService.GetDailyAppointments(Arg.Any<IEnumerable<ICalendarNotification>>()).Returns(summary);
        var worker = CreateWorker(getter, sender, filter, dailyService);

        worker.GetAppointments(cts.Token);

        sender.DidNotReceiveWithAnyArgs().SendNotification(default!, default);
    }

    [Fact]
    public void CheckReminders_ReadsFromStore_FiltersAndSendsExpiringNotifications()
    {
        var due = CreateNotification("due");
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var dailyService = Substitute.For<IDailyAppointmentService>();
        var store = new AppointmentStore();
        store.SetAppointments([due]);
        filter.GetReminders(Arg.Any<IEnumerable<ICalendarNotification>>()).Returns([due]);
        var worker = CreateWorker(getter, sender, filter, dailyService, store);

        worker.CheckReminders(CancellationToken.None);

        getter.DidNotReceiveWithAnyArgs().GetNotifications(default, default);
        filter.Received(1).GetReminders(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(new[] { due })));
        sender.Received(1).SendNotification(due, true);
    }

    [Fact]
    public void CheckReminders_ContinuesSending_WhenOneNotificationFails()
    {
        var first = CreateNotification("first");
        var second = CreateNotification("second");
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var dailyService = Substitute.For<IDailyAppointmentService>();
        var store = new AppointmentStore();
        store.SetAppointments([first, second]);
        filter.GetReminders(Arg.Any<IEnumerable<ICalendarNotification>>()).Returns([first, second]);
        sender.When(i => i.SendNotification(first, true)).Do(_ => throw new InvalidOperationException("Toast failed"));
        var worker = CreateWorker(getter, sender, filter, dailyService, store);

        worker.CheckReminders(CancellationToken.None);

        sender.Received(1).SendNotification(first, true);
        sender.Received(1).SendNotification(second, true);
    }

    [Fact]
    public void CheckReminders_DoesNotSend_WhenCancellationIsAlreadyRequested()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var due = CreateNotification("due");
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var dailyService = Substitute.For<IDailyAppointmentService>();
        var store = new AppointmentStore();
        store.SetAppointments([due]);
        filter.GetReminders(Arg.Any<IEnumerable<ICalendarNotification>>()).Returns([due]);
        var worker = CreateWorker(getter, sender, filter, dailyService, store);

        worker.CheckReminders(cts.Token);

        sender.DidNotReceiveWithAnyArgs().SendNotification(default!, default);
    }

    private static NotifyWorker CreateWorker(
        IGetNotifications<ICalendarNotification> getter,
        ISendNotifications<ICalendarNotification> sender,
        IReminderFilterService<ICalendarNotification> filter,
        IDailyAppointmentService dailyAppointmentService,
        AppointmentStore? store = null) =>
        new(getter, sender, filter, dailyAppointmentService, store ?? new AppointmentStore(), Substitute.For<ILogger<NotifyWorker>>());

    private static ICalendarNotification CreateNotification(string id) =>
        new CalendarNotification { Id = id, Subject = id, Start = DateTime.Today.AddHours(10) };
}
