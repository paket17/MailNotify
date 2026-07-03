using MailNotify.Interfaces;
using Microsoft.Toolkit.Uwp.Notifications;

namespace MailNotify.Services;

public class SendUwpNotify(INotificationCache notifyCache) : ISendNotifications<ICalendarNotification>
{
    public void SendNotification(ICalendarNotification notification, bool isExpired)
    {
        notifyCache.Add(notification, NotificationCacheKind.Configured);
        var toast = CreateToast(notification);
        if (isExpired)
            toast.Show(toastNotification => toastNotification.ExpirationTime = notification.Start);
        else
            toast.Show();
    }

    internal static ToastContentBuilder CreateToast(ICalendarNotification notification)
    {
        var toast = new ToastContentBuilder()
            .AddText(notification.Subject);

        if (string.IsNullOrWhiteSpace(notification.Message))
        {
            toast
                .AddText(notification.Location)
                .AddText($"{notification.Start:HH:mm} - {(notification.Start + notification.Duration):HH:mm}");
        }
        else
        {
            toast.AddText(notification.Message);
        }

        toast
            .SetToastScenario(ToastScenario.IncomingCall)
            .AddAudio(null, silent: true);

        return Uri.TryCreate(notification.WebUrl, UriKind.Absolute, out var activationUri)
            ? toast.SetProtocolActivation(activationUri)
            : toast;
    }
}
