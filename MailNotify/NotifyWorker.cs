using MailNotify.Interfases;

namespace MailNotify;

public class NotifyWorker(IGetNotifications<ICalendarNotification> notifyGetter, 
    ISendNotifications<ICalendarNotification> notifySender,
    INotifyCache notifyCache)
{
    public async Task Run()
    {
        var notifications = notifyGetter.GetNotifications()
            .Where(x => !notifyCache.IsSent(x));
        foreach (var notification in notifications)
            notifySender.SendNotification(notification);

        await Task.Delay(TimeSpan.FromMinutes(5));
    }
}
