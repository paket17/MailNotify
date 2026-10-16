using MailNotify.Entities;
using MailNotify.Interfaces;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MailNotify.Services;

public static class MailNotifyCoreServiceCollectionExtensions
{
    public static IServiceCollection AddMailNotifyCore(this IServiceCollection services)
    {
        services.AddHostedService<Worker>();

        services.AddScoped<ExchangeWebService>();
        services.AddScoped<ExchangeService>(provider =>
            provider.GetRequiredService<ExchangeWebService>().GetExchangeService());
        services.AddScoped<IGetNotifications<ICalendarNotification>, ExchangeCalendarNotify>();
        services.AddScoped<NotifyWorker>();
        services.AddSingleton<ISettingsProvider, SettingsProvider>();
        services.AddSingleton<INotificationHashBuilder<INotification>, NotificationHashBuilder>();
        services.AddSingleton<INotificationHashBuilder<ICalendarNotification>, CalendarNotificationHashBuilder>();
        services.AddSingleton<INotificationHashBuilderResolver, NotificationHashBuilderResolver>();
        services.AddSingleton<INotificationCache, NotificationCache>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<DailyAppointmentNotificationState>();
        services.AddSingleton<AppointmentStore>();
        services.AddScoped<IReminderFilterService<ICalendarNotification>, ReminderFilterService>();
        services.AddScoped<IDailyAppointmentService, DailyAppointmentService>();

        return services;
    }
}
