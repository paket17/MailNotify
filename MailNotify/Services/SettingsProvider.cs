namespace MailNotify.Services;

public class SettingsProvider(IConfiguration configuration)
{
    public double ReminderOffsetMinutes
    {
        get
        {
            var value = configuration.GetValue<double>(nameof(ReminderOffsetMinutes));
            return value == default
                ? 15
                : value;
        }
    }

    public string ExchangeUrl => configuration.GetValue<string>(nameof(ExchangeUrl)) ?? string.Empty;
}
