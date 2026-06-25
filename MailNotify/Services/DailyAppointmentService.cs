using MailNotify.Entities;
using MailNotify.Interfaces;

namespace MailNotify.Services;

internal class DailyAppointmentService(
    ISettingsProvider settingsProvider,
    INotificationCache notifyCache
) : IDailyAppointmentService
{
    private const int MaxAppointmentsInMessage = 3;

    public ICalendarNotification? GetDailyAppointments(IEnumerable<ICalendarNotification> calendarNotifications)
    {
        if (!settingsProvider.NotifyDailyAppointments)
            return null;

        var now = DateTime.Now;
        var notifications = calendarNotifications
            .Where(n => n.Start > now)
            .Where(n => !notifyCache.Contains(n, NotificationCacheKind.Daily))
            .ToList();
        foreach (var notification in notifications)
            notifyCache.Add(notification, NotificationCacheKind.Daily);

        return CreateDailyAppointmentsNotification(notifications, now);
    }

    private static CalendarNotification? CreateDailyAppointmentsNotification(List<ICalendarNotification> appointments, DateTime start)
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

        return new CalendarNotification()
        {
            Id = $"today-appointments:{start:yyyy-MM-dd:HH-mm-ss}",
            Subject = "New appointments for today",
            Message = string.Join(Environment.NewLine, appointmentLines),
            Start = start,
        };
    }

    private static List<string> GetPreparedAppointmentTexts(IEnumerable<ICalendarNotification> appointments)
    {
        return [.. appointments.Select(i => $"{i.Start:HH:mm} {i.Subject}")];
    }
}
