using Microsoft.Extensions.Configuration;

namespace MailNotify.Services;

public class TraySettingsProvider(IConfiguration configuration) : ITraySettingsProvider
{
    public string CalendarUrl =>
        configuration.GetValue<string>(nameof(CalendarUrl))
        ?? configuration.GetValue<string>("CalendarPath")
        ?? string.Empty;
}
