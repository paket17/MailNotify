namespace MailNotify.Interfaces;

public interface ISendNotifications<in T> 
    where T : INotification
{
    public void SendNotification(T notification);
}
