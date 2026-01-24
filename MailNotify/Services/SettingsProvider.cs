using MailNotify.Interfaces;

namespace MailNotify.Services;

public class SettingsProvider(IConfiguration configuration) : ISettingsProvider
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

    public double UpdateOffsetMinutes
    {
        get
        {
            var value = configuration.GetValue<double>(nameof(UpdateOffsetMinutes));
            return value == default
                ? 3
                : value;
        }
    }

    public string ExchangeUrl => configuration.GetValue<string>(nameof(ExchangeUrl)) ?? string.Empty;
}
