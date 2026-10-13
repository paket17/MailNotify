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
            toast.AddText($"{notification.Start:HH:mm} - {(notification.Start + notification.Duration):HH:mm}");
            if (!string.IsNullOrWhiteSpace(notification.Location))
                toast.AddText(notification.Location);
        }
        else
        {
            var lines = notification.Message.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var group = new AdaptiveGroup();
            var subgroup = new AdaptiveSubgroup();
            foreach (var line in lines)
                subgroup.Children.Add(new AdaptiveText { Text = line });

            group.Children.Add(subgroup);
            toast.AddVisualChild(group);
        }

        toast
            .SetToastScenario(ToastScenario.IncomingCall)
            .AddAudio(null, silent: true);

        return Uri.TryCreate(notification.WebUrl, UriKind.Absolute, out var activationUri)
            ? toast.SetProtocolActivation(activationUri)
            : toast;
    }
}
