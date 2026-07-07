namespace MailNotify.Interfaces;

public interface INotificationHashBuilder<in TNotification>
    where TNotification : INotification
{
    string Build(TNotification notification);
}
