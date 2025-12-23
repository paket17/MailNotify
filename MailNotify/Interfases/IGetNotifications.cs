namespace MailNotify.Interfases;

public interface IGetNotifications<out T> where T : INotification
{
    public IEnumerable<T> GetNotifications();
}
