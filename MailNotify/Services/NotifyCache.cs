using MailNotify.Interfases;

namespace MailNotify.Services;

public class NotifyCache : INotifyCache
{
    private readonly Dictionary<string, DateTime> notifyCache;
    private DateTime lastDate;

    public bool IsSent(INotification notification)
    {
        ClearCache();
        var isContains = notifyCache.TryGetValue(notification.Id, out var lastUpdate) 
            && lastUpdate == notification.LastUpdate;
        if (!isContains)
        {
            if (!notifyCache.TryAdd(notification.Id, notification.LastUpdate))
                notifyCache[notification.Id] = notification.LastUpdate;
        }

        return isContains;
    }

    private void ClearCache()
    {
        if (lastDate.Date != DateTime.Today.Date)
            notifyCache.Clear();

        lastDate = DateTime.Today;
    }

    public NotifyCache()
    {
        notifyCache = [];
    }
}
