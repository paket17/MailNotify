using MailNotify.Interfaces;

namespace MailNotify.Services;

public static class MailNotifyServiceCollectionExtensions
{
    public static IServiceCollection AddMailNotifyServices(this IServiceCollection services)
    {
        if (OperatingSystem.IsWindows())
        {
            services.AddScoped<ISendNotifications<ICalendarNotification>, SendUwpNotify>();
            services.AddSingleton<IAutoStartService, WindowsAutoStartService>();
            services.AddSingleton<IWindowsThemeService, WindowsThemeService>();
            services.AddSingleton<ITraySettingsProvider, TraySettingsProvider>();
            services.AddSingleton<ITrayMenuPresenter, TrayMenuPresenter>();
            services.AddSingleton<ITrayIconManager, TrayIconManager>();
            services.AddHostedService<WindowsTrayService>();
        }

        services.AddMailNotifyCore();
        return services;
    }
}
