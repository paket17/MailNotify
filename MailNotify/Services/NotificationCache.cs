using MailNotify.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MailNotify.Services;

public class NotificationCache(
    ISettingsProvider settingsProvider,
    INotificationHashBuilderResolver notificationHashBuilder) : INotificationCache
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions());
    private readonly TimeSpan ttlConfigure = TimeSpan.FromMinutes(settingsProvider.ReminderOffsetMinutes);
    private readonly TimeSpan ttlDaily = TimeSpan.FromDays(1);

    public bool Add(INotification notify, NotificationCacheKind cacheKind)
    {
        if (cache.TryGetValue(GetKey(notify, cacheKind), out _))
            return false;

        var ttl = cacheKind == NotificationCacheKind.Daily ? ttlDaily : ttlConfigure;
        cache.Set(GetKey(notify, cacheKind), true, ttl);
        return true;
    }

    public bool Contains(INotification notify, NotificationCacheKind kind) =>
        cache.TryGetValue(GetKey(notify, kind), out _);

    public void Remove(INotification notify, NotificationCacheKind kind) =>
        cache.Remove(GetKey(notify, kind));

    private string GetKey(INotification notification, NotificationCacheKind kind)
    {
        return $"{kind}:{notification.Id}:{notificationHashBuilder.Build(notification)}";
    }
}
