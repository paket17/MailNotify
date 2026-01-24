namespace MailNotify.Interfaces;

public interface INotifyCache
{
    public bool Add(INotification notification);
    public bool Contains(INotification notification);
    public void Remove(INotification notification);
}
