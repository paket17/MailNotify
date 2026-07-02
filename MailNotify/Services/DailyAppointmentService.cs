using MailNotify.Entities;
using MailNotify.Interfaces;

namespace MailNotify.Services;

internal class DailyAppointmentService(
    ISettingsProvider settingsProvider,
    INotificationCache notifyCache,
    TimeProvider timeProvider,
    DailyAppointmentNotificationState state
) : IDailyAppointmentService
{
    private const int MaxAppointmentsInMessage = 3;

    public ICalendarNotification? GetDailyAppointments(IEnumerable<ICalendarNotification> calendarNotifications)
    {
        if (!settingsProvider.NotifyDailyAppointments)
            return null;

        var now = timeProvider.GetLocalNow().DateTime;
        var notifications = calendarNotifications
            .Where(n => n.Start > now)
            .Where(n => !notifyCache.Contains(n, NotificationCacheKind.Daily))
            .ToList();
        foreach (var notification in notifications)
            notifyCache.Add(notification, NotificationCacheKind.Daily);

        return CreateDailyAppointmentsNotification(notifications, now);
    }

    private CalendarNotification? CreateDailyAppointmentsNotification(List<ICalendarNotification> appointments, DateTime start)
    {
        if (appointments.Count == 0)
            return null;

        var appointmentLines = GetPreparedAppointmentTexts(appointments.Take(MaxAppointmentsInMessage));
        if (appointments.Count > MaxAppointmentsInMessage)
        {
            appointmentLines.Add($"and {appointments.Count - MaxAppointmentsInMessage} more");
            var moreAppointments = GetPreparedAppointmentTexts(appointments.Skip(MaxAppointmentsInMessage));
            appointmentLines.AddRange(moreAppointments);
        }

        var subject = IsFirstDailyAppointmentNotification()
            ? "Appointments for today"
            : "New appointments for today";
        return new CalendarNotification()
        {
            Id = $"today-appointments:{start:yyyy-MM-dd:HH-mm-ss}",
            Subject = subject,
            Message = string.Join(Environment.NewLine, appointmentLines),
            Start = start,
        };
    }

    private static List<string> GetPreparedAppointmentTexts(IEnumerable<ICalendarNotification> appointments)
    {
        return [.. appointments.Select(i => $"{i.Start:HH:mm} {i.Subject}")];
    }

    private bool IsFirstDailyAppointmentNotification()
    {
        var today = timeProvider.GetLocalNow().Date;
        if (state.LastNotificationDate != today)
        {
            state.LastNotificationDate = today;
            return true;
        }

        return false;
    }
}
