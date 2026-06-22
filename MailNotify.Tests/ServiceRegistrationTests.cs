using MailNotify.Interfaces;
using MailNotify.Services;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MailNotify.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddMailNotifyServices_RegistersResolvableNotificationWorkerGraph()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExchangeUrl"] = "https://exchange.test/EWS/Exchange.asmx",
                ["ReminderOffsetMinutes"] = "15",
                ["UpdateOffsetMinutes"] = "3",
                ["AutoStart"] = "false",
                ["NotifyDailyAppointments"] = "true"
            })
            .Build());
        services.AddMailNotifyServices();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<NotifyWorker>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IGetNotifications<ICalendarNotification>>()
            .Should().BeOfType<ExchangeCalendarNotify>();
        scope.ServiceProvider.GetRequiredService<IDailyAppointmentService>()
            .Should().BeOfType<DailyAppointmentService>();
        scope.ServiceProvider.GetRequiredService<INotificationCache>()
            .Should().BeOfType<NotificationCache>();
        scope.ServiceProvider.GetRequiredService<ExchangeService>().Url
            .Should().Be(new Uri("https://exchange.test/EWS/Exchange.asmx"));
    }
}
