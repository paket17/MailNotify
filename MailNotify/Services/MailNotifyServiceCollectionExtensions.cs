using MailNotify.Interfaces;
using Microsoft.Exchange.WebServices.Data;

namespace MailNotify.Services;

public static class MailNotifyServiceCollectionExtensions
{
    public static IServiceCollection AddMailNotifyServices(this IServiceCollection services)
    {
        services.AddHostedService<Worker>();

        services.AddScoped<ExchangeWebService>();
        services.AddScoped<ExchangeService>(provider =>
            provider.GetRequiredService<ExchangeWebService>().GetExchangeService());
        services.AddScoped<IGetNotifications<ICalendarNotification>, ExchangeCalendarNotify>();
        services.AddScoped<ISendNotifications<ICalendarNotification>, SendUwpNotify>();
        services.AddScoped<NotifyWorker>();
        services.AddSingleton<IAutoStartService, WindowsAutoStartService>();
        services.AddSingleton<ISettingsProvider, SettingsProvider>();
        services.AddSingleton<INotificationHashBuilder<INotification>, NotificationHashBuilder>();
        services.AddSingleton<INotificationHashBuilder<ICalendarNotification>, CalendarNotificationHashBuilder>();
        services.AddSingleton<INotificationHashBuilderResolver, NotificationHashBuilderResolver>();
        services.AddSingleton<INotificationCache, NotificationCache>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<DailyAppointmentNotificationState>();
        services.AddScoped<IReminderFilterService<ICalendarNotification>, ReminderFilterService>();
        services.AddScoped<IDailyAppointmentService, DailyAppointmentService>();

        return services;
    }
}
