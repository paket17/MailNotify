using MailNotify.Interfaces;

namespace MailNotify;

public class NotifyWorker(
    IGetNotifications<ICalendarNotification> notifyGetter, 
    ISendNotifications<ICalendarNotification> notifySender,
    IReminderFilterService<ICalendarNotification> reminderFilterService,
    IDailyAppointmentService dailyAppointmentService,
    ILogger<NotifyWorker> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var start = DateTime.Today;
        var end = DateTime.Today.AddDays(1).AddSeconds(-1);
        var expiringNotifications = new List<ICalendarNotification>();
        var notifications = new List<ICalendarNotification>();

        TryCatch(
            () =>
            {
                var todayNotifications = notifyGetter.GetNotifications(start, end).ToList();
                var dailyAppointmentsNotification = dailyAppointmentService.GetDailyAppointments(todayNotifications);
                if (dailyAppointmentsNotification != null)
                    notifications.Add(dailyAppointmentsNotification);

                expiringNotifications.AddRange(reminderFilterService.GetReminders(todayNotifications));
            },
            ex => logger.LogError(ex, "Failed to get notifications"));

        SendNotification(notifications, (n) => notifySender.SendNotification(n, false), cancellationToken);
        SendNotification(expiringNotifications, (n) => notifySender.SendNotification(n, true), cancellationToken);

        await Task.CompletedTask;
    }

    private void SendNotification(List<ICalendarNotification> notifications, Action<ICalendarNotification> sendAction, CancellationToken cancellationToken)
    {
        foreach (var notification in notifications)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            TryCatch(
                () => sendAction(notification),
                (ex) => logger.LogError(ex, "Failed to send notification {NotificationId}", notification?.Id));
        }
    }

    private static void TryCatch(Action action, Action<Exception> catchAction)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            catchAction(ex);
        }
    }
}
