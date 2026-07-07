using MailNotify.Interfaces;

namespace MailNotify.Services;

internal class ReminderFilterService(
    INotificationCache notifyCache,
    ISettingsProvider settingsProvider) 
    : IReminderFilterService<ICalendarNotification>
{
    private readonly TimeSpan reminderOffset = TimeSpan.FromMinutes(settingsProvider.ReminderOffsetMinutes);

    public IEnumerable<ICalendarNotification> GetReminders(IEnumerable<ICalendarNotification> notifications)
    {
        return [.. notifications
            .Where(i => DateTime.Now >= i.Start - reminderOffset && DateTime.Now < i.Start)
            .Where(i => !notifyCache.Contains(i, NotificationCacheKind.Configured))];
    }
}
