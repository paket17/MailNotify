using MailNotify.Interfaces;
using MailNotify.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MailNotify.Tests;

public class WorkerTests
{
    [Fact]
    public async Task Worker_ExecutesSyncAndReminders_AndStopsCleanlyOnCancellation()
    {
        var logger = Substitute.For<ILogger<Worker>>();
        var settings = Substitute.For<ISettingsProvider>();
        settings.UpdateOffsetMinutes.Returns(60);

        var getter = Substitute.For<IGetNotifications<ICalendarNotification>>();
        getter.GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([]);

        var sender = Substitute.For<ISendNotifications<ICalendarNotification>>();
        var filter = Substitute.For<IReminderFilterService<ICalendarNotification>>();
        var dailyService = Substitute.For<IDailyAppointmentService>();
        var store = new AppointmentStore();
        var notifyWorker = new NotifyWorker(getter, sender, filter, dailyService, store, Substitute.For<ILogger<NotifyWorker>>());

        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(NotifyWorker)).Returns(notifyWorker);
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var worker = new Worker(logger, settings, scopeFactory);

        using var cts = new CancellationTokenSource();
        var startTask = worker.StartAsync(cts.Token);

        // Allow loop to perform initial sync iteration
        await Task.Delay(50);
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        getter.Received().GetNotifications(Arg.Any<DateTime>(), Arg.Any<DateTime>());
        scopeFactory.Received().CreateScope();
    }
}
