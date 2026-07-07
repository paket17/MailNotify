namespace MailNotify.Interfaces;

public interface ISettingsProvider
{
    public double ReminderOffsetMinutes { get; }
    public double UpdateOffsetMinutes { get; }
    public string MailUrl { get; }
    public bool AutoStart { get; }
    public bool NotifyDailyAppointments { get; }
}
