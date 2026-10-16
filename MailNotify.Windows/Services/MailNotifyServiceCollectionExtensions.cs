using MailNotify.Interfaces;

namespace MailNotify.Services;

public static class MailNotifyServiceCollectionExtensions
{
    public static IServiceCollection AddMailNotifyServices(this IServiceCollection services)
    {
        services.AddMailNotifyCore();
        services.AddScoped<ISendNotifications<ICalendarNotification>, SendUwpNotify>();
        services.AddSingleton<IAutoStartService, WindowsAutoStartService>();

        return services;
    }
}
