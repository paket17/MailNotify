using MailNotify.Interfaces;

namespace MailNotify;

public class NotifyWorker(IGetNotifications<ICalendarNotification> notifyGetter, 
    ISendNotifications<ICalendarNotification> notifySender,
    ILogger<NotifyWorker> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        IEnumerable<ICalendarNotification> notifications;
        try
        {
            notifications = notifyGetter.GetNotifications();
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
