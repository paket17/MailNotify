using MailNotify.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MailNotify.Services;

public class NotifyCache : INotifyCache
{
    private readonly MemoryCache cache;
    private readonly TimeSpan ttl;

    public bool Add(INotification notify)
    {
        if (cache.TryGetValue(notify, out _))
            return false;

        cache.Set(notify, true, ttl);
        return true;
    }

    public bool Contains(INotification notify) => 
        cache.TryGetValue(notify, out _);

    public void Remove(INotification notify) => 
        cache.Remove(notify);

    public NotifyCache(ISettingsProvider settingsProvider)
    {
        ttl = TimeSpan.FromMinutes(settingsProvider.ReminderOffsetMinutes);
        cache = new MemoryCache(new MemoryCacheOptions());
    }
}
