namespace MailNotify.Interfaces;

public interface IReminderFilterService<T> where T : INotification
{
    IEnumerable<T> GetReminders(IEnumerable<T> notifications);
}
