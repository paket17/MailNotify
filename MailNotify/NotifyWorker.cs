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
        var notifications = new List<ICalendarNotification>();

        try
        {
            var todayNotifications = notifyGetter.GetNotifications(start, end).ToList();
            var dailyAppointmentsNotification = dailyAppointmentService.GetDailyAppointments(todayNotifications);
            if (dailyAppointmentsNotification != null)
                notifications.Add(dailyAppointmentsNotification);

            notifications.AddRange(reminderFilterService.GetReminders(todayNotifications));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get notifications");
            return;
        }

        foreach (var notification in notifications)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                notifySender.SendNotification(notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification {NotificationId}", notification?.Id);
            }
        }

        await Task.CompletedTask;
    }
}
