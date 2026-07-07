using MailNotify.Interfaces;

namespace MailNotify.Services;

public class SettingsProvider(IConfiguration configuration) : ISettingsProvider
{
    private const string ExchangePath = "EWS/Exchange.asmx";

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

    public string MailUrl
    {
        get
        {
            var value = configuration.GetValue<string>(nameof(MailUrl)) ?? string.Empty;
            if (value == string.Empty)
                return value;

            value = value.EndsWith('/') ? value + ExchangePath : value + "/" + ExchangePath;
            return value;
        }
    }

    public bool AutoStart => configuration.GetValue<bool?>(nameof(AutoStart)) ?? true;

    public bool NotifyDailyAppointments => configuration.GetValue<bool?>(nameof(NotifyDailyAppointments)) ?? true;
}
