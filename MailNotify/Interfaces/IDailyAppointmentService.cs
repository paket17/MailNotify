namespace MailNotify.Interfaces;

public interface IDailyAppointmentService
{
    ICalendarNotification? GetDailyAppointments(IEnumerable<ICalendarNotification> calendarNotifications);
}
