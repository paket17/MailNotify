using MailNotify.Interfaces;
using Microsoft.Toolkit.Uwp.Notifications;

namespace MailNotify.Services;

public class SendUwpNotify(INotificationCache notifyCache) : ISendNotifications<ICalendarNotification>
{
    private const int MaxSubjectLength = 35;

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
        var toast = new ToastContentBuilder();

        if (string.IsNullOrWhiteSpace(notification.Message))
        {
            toast.AddText(PrepareSubject(notification.Subject));
            toast.AddText($"{notification.Start:HH:mm} - {(notification.Start + notification.Duration):HH:mm}");
            if (!string.IsNullOrWhiteSpace(notification.Location))
                toast.AddText(notification.Location);
        }
        else
        {
            toast.AddText(notification.Subject);
            toast.AddText(notification.Message, hintWrap: true);
        }

        toast
            .SetToastScenario(ToastScenario.IncomingCall)
            .AddAudio(null, silent: true);

        return Uri.TryCreate(notification.WebUrl, UriKind.Absolute, out var activationUri)
            ? toast.SetProtocolActivation(activationUri)
            : toast;
    }

    private static string PrepareSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return string.Empty;

        var cleaned = subject.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (cleaned.Length > MaxSubjectLength)
            return cleaned[..(MaxSubjectLength - 1)] + "…";

        return cleaned;
    }
}
