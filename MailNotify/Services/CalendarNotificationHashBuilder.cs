using MailNotify.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MailNotify.Services;

public class CalendarNotificationHashBuilder : INotificationHashBuilder<ICalendarNotification>
{
    public string Build(ICalendarNotification notification)
    {
        var end = notification.Start + notification.Duration;
        var data = string.Join(':',
            notification.Subject,
            notification.Start.ToString("O", CultureInfo.InvariantCulture),
            end.ToString("O", CultureInfo.InvariantCulture),
            notification.Location);

        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(data)));
    }
}
