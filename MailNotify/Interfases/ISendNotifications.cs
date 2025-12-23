namespace MailNotify.Interfases;

public interface ISendNotifications<in T> 
    where T : INotification
{
    public void SendNotification(T notification);
}
