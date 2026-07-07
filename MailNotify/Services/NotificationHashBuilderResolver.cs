using MailNotify.Interfaces;

namespace MailNotify.Services;

internal class NotificationHashBuilderResolver(
    INotificationHashBuilder<INotification> notificationHashBuilder,
    INotificationHashBuilder<ICalendarNotification> calendarHashBuilder)
    : INotificationHashBuilderResolver
{
    public string Build(INotification notification)
    {
        return notification switch
        {
            ICalendarNotification calendar => calendarHashBuilder.Build(calendar),
            _ => notificationHashBuilder.Build(notification)
        };
    }
}
