using MailNotify.Interfaces;
using MailNotify.Services;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                ["MailUrl"] = "https://exchange.test",
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
        scope.ServiceProvider.GetRequiredService<TimeProvider>()
            .Should().Be(TimeProvider.System);
        scope.ServiceProvider.GetRequiredService<INotificationCache>()
            .Should().BeOfType<NotificationCache>();
        scope.ServiceProvider.GetRequiredService<INotificationHashBuilder<INotification>>()
            .Should().BeOfType<NotificationHashBuilder>();
        scope.ServiceProvider.GetRequiredService<INotificationHashBuilder<ICalendarNotification>>()
            .Should().BeOfType<CalendarNotificationHashBuilder>();
        scope.ServiceProvider.GetRequiredService<INotificationHashBuilderResolver>()
            .Should().BeOfType<NotificationHashBuilderResolver>();
        scope.ServiceProvider.GetRequiredService<AppointmentStore>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ExchangeService>().Url
            .Should().Be(new Uri("https://exchange.test/EWS/Exchange.asmx"));

        var hostedServices = provider.GetServices<IHostedService>();
        hostedServices.Should().Contain(s => s is Worker);
        if (OperatingSystem.IsWindows())
        {
            provider.GetRequiredService<IWindowsThemeService>().Should().BeOfType<WindowsThemeService>();
            provider.GetRequiredService<ITrayMenuPresenter>().Should().BeOfType<TrayMenuPresenter>();
            provider.GetRequiredService<ITrayIconManager>().Should().BeOfType<TrayIconManager>();
            hostedServices.Should().Contain(s => s is WindowsTrayService);
        }
        else
        {
            hostedServices.Should().NotContain(s => s is WindowsTrayService);
        }
    }

    [Fact]
    public void AddMailNotifyCore_RegistersResolvableWorkerGraph_WithoutPlatformServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailUrl"] = "https://exchange.test",
                ["ReminderOffsetMinutes"] = "15",
                ["UpdateOffsetMinutes"] = "3",
                ["AutoStart"] = "false",
                ["NotifyDailyAppointments"] = "true"
            })
            .Build());
        services.AddMailNotifyCore();
        services.AddScoped(_ => Substitute.For<ISendNotifications<ICalendarNotification>>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<NotifyWorker>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<AppointmentStore>().Should().NotBeNull();

        var hostedServices = provider.GetServices<IHostedService>();
        hostedServices.Should().Contain(s => s is Worker);
        hostedServices.Should().NotContain(s => s is WindowsTrayService);
    }

    [Fact]
    public void AddMailNotifyServices_RegistersDailyAppointmentStateAsSingletonAndServiceAsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailUrl"] = "https://exchange.test",
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
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstService = firstScope.ServiceProvider.GetRequiredService<IDailyAppointmentService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<IDailyAppointmentService>();
        var firstState = firstScope.ServiceProvider.GetRequiredService<DailyAppointmentNotificationState>();
        var secondState = secondScope.ServiceProvider.GetRequiredService<DailyAppointmentNotificationState>();
        var firstStore = firstScope.ServiceProvider.GetRequiredService<AppointmentStore>();
        var secondStore = secondScope.ServiceProvider.GetRequiredService<AppointmentStore>();

        firstService.Should().NotBeSameAs(secondService);
        firstState.Should().BeSameAs(secondState);
        firstStore.Should().BeSameAs(secondStore);
    }
}
