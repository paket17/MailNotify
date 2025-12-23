using MailNotify.Interfases;
using Microsoft.Toolkit.Uwp.Notifications;

namespace MailNotify.Services;

public class SendUwpNotify : ISendNotifications<ICalendarNotification>
{
    public void SendNotification(ICalendarNotification notification)
    {
        var toast = CreateToast(notification);
        toast.Show();
    }

    private ToastContentBuilder CreateToast(ICalendarNotification notification)
    {
        return new ToastContentBuilder()
            .AddText(notification.Subject)
            .AddText(notification.Location)
            .AddText($"{notification.Start:HH:mm} - {(notification.Start + notification.Duration):HH:mm}")
            .SetToastScenario(ToastScenario.IncomingCall)
            .AddAudio(null, silent: true)
            .SetProtocolActivation(new(notification.WebUrl ?? string.Empty));
    }
}