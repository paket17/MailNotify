using MailNotify.Interfases;

namespace MailNotify.Entities;

public class CalendarNotification : Notification, ICalendarNotification
{
    public DateTime Start { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Location { get; set; }
    public string? WebUrl { get; set; }
}
