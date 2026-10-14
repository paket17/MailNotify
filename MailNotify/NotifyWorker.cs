using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify;

public class NotifyWorker(
    IGetNotifications<ICalendarNotification> notifyGetter, 
    ISendNotifications<ICalendarNotification> notifySender,
    IReminderFilterService<ICalendarNotification> reminderFilterService,
    IDailyAppointmentService dailyAppointmentService,
    AppointmentStore appointmentStore,
    ILogger<NotifyWorker> logger)
{
    public void GetAppointments(CancellationToken cancellationToken)
    {
        var start = DateTime.Today;
        var end = DateTime.Today.AddDays(1).AddSeconds(-1);
        var notifications = new List<ICalendarNotification>();

        TryCatch(
            () =>
            {
                var todayNotifications = notifyGetter.GetNotifications(start, end).ToList();
                appointmentStore.SetAppointments(todayNotifications);

                var dailyAppointmentsNotification = dailyAppointmentService.GetDailyAppointments(todayNotifications);
                if (dailyAppointmentsNotification != null)
                    notifications.Add(dailyAppointmentsNotification);

                SendNotification(notifications, (n) => notifySender.SendNotification(n, false), cancellationToken);
            },
            ex => logger.LogError(ex, "Failed to get notifications"));
    }

    public void CheckReminders(CancellationToken cancellationToken)
    {
        var expiringNotifications = new List<ICalendarNotification>();

        TryCatch(
            () =>
            {
                var storedAppointments = appointmentStore.GetAppointments();
                expiringNotifications.AddRange(reminderFilterService.GetReminders(storedAppointments));
            },
            ex => logger.LogError(ex, "Failed to filter reminders"));

        SendNotification(expiringNotifications, (n) => notifySender.SendNotification(n, true), cancellationToken);
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
