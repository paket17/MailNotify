using MailNotify.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MailNotify.Services;

public class NotificationHashBuilder : INotificationHashBuilder<INotification>
{
    public string Build(INotification notification)
    {
        var data = $"{notification.Subject}:{notification.Message}";
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(data)));
    }
}
