namespace MailNotify.Interfaces;

public interface ISettingsProvider
{
    public double ReminderOffsetMinutes { get; }
    public double UpdateOffsetMinutes { get; }
    public string ExchangeUrl { get; }
}
