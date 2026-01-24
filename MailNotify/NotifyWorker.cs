using MailNotify.Interfaces;

namespace MailNotify;

public class NotifyWorker(IGetNotifications<ICalendarNotification> notifyGetter, 
    ISendNotifications<ICalendarNotification> notifySender)
{
    public async Task Run()
    {
        var notifications = notifyGetter.GetNotifications();
        foreach (var notification in notifications)
            notifySender.SendNotification(notification);
    }
}
