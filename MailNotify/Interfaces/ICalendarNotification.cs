namespace MailNotify.Interfaces;

public interface ICalendarNotification : INotification
{
    DateTime Start { get; set; }
    TimeSpan Duration { get; set; }
    string? Location { get; set; }
    string? WebUrl { get; set; }
}
