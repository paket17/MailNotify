using MailNotify.Entities;
using MailNotify.Interfaces;
using Microsoft.Extensions.Logging;

namespace MailNotify.Tests;

public class NotifyWorkerTests
{
    [Fact]
    public async Task Run_GetsTodayNotifications_FiltersThem_AndSendsReminders()
    {
        var source = new[]
        {
            CreateNotification("source"),
            CreateNotification("filtered")
        };
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var newTodayAppointmentService = Substitute.For<IDailyAppointmentService>();
        var start = DateTime.Today;
        var end = start.AddDays(1).AddSeconds(-1);
        getter.GetNotifications(start, end).Returns(source);
        filter.GetReminders(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(source)))
            .Returns([source[1]]);
        var worker = CreateWorker(getter, sender, filter, newTodayAppointmentService);

        await worker.Run(CancellationToken.None);

        getter.Received(1).GetNotifications(start, end);
        newTodayAppointmentService.Received(1)
            .GetDailyAppointments(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(source)));
        filter.Received(1)
            .GetReminders(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(source)));
        sender.Received(1).SendNotification(source[1], true);
        sender.DidNotReceive().SendNotification(source[0], false);
    }

    [Fact]
    public async Task Run_ReturnsWithoutSending_WhenGettingNotificationsFails()
    {
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var newTodayAppointmentService = Substitute.For<IDailyAppointmentService>();
        getter.GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(_ => throw new InvalidOperationException("Exchange failed"));
        var worker = CreateWorker(getter, sender, filter, newTodayAppointmentService);

        await worker.Run(CancellationToken.None);

        newTodayAppointmentService.DidNotReceiveWithAnyArgs().GetDailyAppointments(default!);
        filter.DidNotReceiveWithAnyArgs().GetReminders(default!);
        sender.DidNotReceiveWithAnyArgs().SendNotification(default!, default);
    }

    [Fact]
    public async Task Run_ContinuesSending_WhenOneNotificationFails()
    {
        var first = CreateNotification("first");
        var second = CreateNotification("second");
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var newTodayAppointmentService = Substitute.For<IDailyAppointmentService>();
        getter.GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([first, second]);
        filter.GetReminders(Arg.Any<IEnumerable<ICalendarNotification>>()).Returns([first, second]);
        sender.When(i => i.SendNotification(first, true)).Do(_ => throw new InvalidOperationException("Toast failed"));
        var worker = CreateWorker(getter, sender, filter, newTodayAppointmentService);

        await worker.Run(CancellationToken.None);

        sender.Received(1).SendNotification(first, true);
        sender.Received(1).SendNotification(second, true);
    }

    [Fact]
    public async Task Run_DoesNotSend_WhenCancellationIsAlreadyRequested()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var newTodayAppointmentService = Substitute.For<IDailyAppointmentService>();
        getter.GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([CreateNotification("cancelled")]);
        filter.GetReminders(Arg.Any<IEnumerable<ICalendarNotification>>())
            .Returns(call => call.Arg<IEnumerable<ICalendarNotification>>());
        var worker = CreateWorker(getter, sender, filter, newTodayAppointmentService);

        await worker.Run(cts.Token);

        sender.DidNotReceiveWithAnyArgs().SendNotification(default!, default);
    }

    [Fact]
    public async Task Run_SendsDailyAppointmentsNotificationBeforeReminders()
    {
        var source = new[]
        {
            CreateNotification("future"),
            CreateNotification("reminder")
        };
        var newAppointments = CreateNotification("new-today-appointments");
        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var newTodayAppointmentService = Substitute.For<IDailyAppointmentService>();
        getter.GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(source);
        newTodayAppointmentService.GetDailyAppointments(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(source)))
            .Returns(newAppointments);
        filter.GetReminders(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(source)))
            .Returns([source[1]]);
        var worker = CreateWorker(getter, sender, filter, newTodayAppointmentService);

        await worker.Run(CancellationToken.None);

        Received.InOrder(() =>
        {
            sender.SendNotification(newAppointments, false);
            sender.SendNotification(source[1], true);
        });
        filter.Received(1)
            .GetReminders(Arg.Is<IEnumerable<ICalendarNotification>>(items => items.SequenceEqual(source)));
        sender.DidNotReceive().SendNotification(source[0], false);
    }

    private static NotifyWorker CreateWorker(
        IGetNotifications<ICalendarNotification> getter,
        ISendNotifications<ICalendarNotification> sender,
        IReminderFilterService<ICalendarNotification> filter,
        IDailyAppointmentService newTodayAppointmentService) =>
        new(getter, sender, filter, newTodayAppointmentService, Substitute.For<ILogger<NotifyWorker>>());

    private static ICalendarNotification CreateNotification(string id) =>
        new CalendarNotification { Id = id, Subject = id, Start = DateTime.Today.AddHours(10) };
}
