using MailNotify.Services;

namespace MailNotify.Interfaces;

public interface INotificationCache
{
    public bool Add(INotification notification, NotificationCacheKind cacheKind);
    public bool Contains(INotification notification, NotificationCacheKind kind);
    public void Remove(INotification notification, NotificationCacheKind kind);
}
