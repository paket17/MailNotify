namespace MailNotify.Interfases;

public interface INotifyCache
{
    public bool IsSent(INotification notification);
}
