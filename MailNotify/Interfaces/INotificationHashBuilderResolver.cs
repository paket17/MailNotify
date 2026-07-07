namespace MailNotify.Interfaces;

public interface INotificationHashBuilderResolver
{
    string Build(INotification notification);
}