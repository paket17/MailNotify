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
    private const int MaxAppointmentsInMessage = 4;
    private const int MaxSubjectLength = 35;

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

        List<string> appointmentLines;
        if (appointments.Count <= MaxAppointmentsInMessage)
            appointmentLines = GetPreparedAppointmentTexts(appointments, truncate: true);
        else
        {
            var visibleCount = MaxAppointmentsInMessage - 1;
            appointmentLines = GetPreparedAppointmentTexts(appointments.Take(visibleCount), truncate: true);
            appointmentLines.Add($"and {appointments.Count - visibleCount} more");
            appointmentLines.AddRange(GetPreparedAppointmentTexts(appointments.Skip(visibleCount), truncate: false));
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

    private static List<string> GetPreparedAppointmentTexts(IEnumerable<ICalendarNotification> appointments, bool truncate)
    {
        return [.. appointments.Select(i => $"{i.Start:HH:mm} {PrepareSubject(i.Subject, truncate)}")];
    }

    private static string PrepareSubject(string? subject, bool truncate)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return string.Empty;

        var cleaned = subject.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (truncate && cleaned.Length > MaxSubjectLength)
            return cleaned[..(MaxSubjectLength - 1)] + "…";

        return cleaned;
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
