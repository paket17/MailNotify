using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MailNotify.Tests;

public class WindowsTrayServiceTests
{
    private readonly ISettingsProvider settingsProvider = Substitute.For<ISettingsProvider>();
    private readonly AppointmentStore appointmentStore = new();
    private readonly IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ILogger<WindowsTrayService> logger = Substitute.For<ILogger<WindowsTrayService>>();
    private readonly IHostApplicationLifetime appLifetime = Substitute.For<IHostApplicationLifetime>();

    [Fact]
    public void GetTooltipText_ReturnsNoUpcomingMeetings_WhenStoreIsEmpty()
    {
        var service = new WindowsTrayService(settingsProvider, appointmentStore, scopeFactory, logger, appLifetime);

        var tooltip = service.GetTooltipText();

        tooltip.Should().Be("MailNotify\nNo upcoming meetings");
    }

    [Fact]
    public void GetTooltipText_ReturnsNextMeetingTimeAndSubject_WhenUpcomingMeetingExists()
    {
        var futureTime = DateTime.Now.AddHours(1);
        appointmentStore.SetAppointments([
            new CalendarNotification
            {
                Subject = "Architecture Review",
                Start = futureTime,
                Duration = TimeSpan.FromMinutes(30)
            }
        ]);

        var service = new WindowsTrayService(settingsProvider, appointmentStore, scopeFactory, logger, appLifetime);

        var tooltip = service.GetTooltipText();

        tooltip.Should().Be($"MailNotify\nNext: {futureTime:HH:mm} (Architecture Review)");
        tooltip.Length.Should().BeLessThanOrEqualTo(63);
    }

    [Fact]
    public void GetTooltipText_TruncatesLongSubject_AndKeepsLengthWithin63Chars()
    {
        var futureTime = DateTime.Now.AddHours(2);
        appointmentStore.SetAppointments([
            new CalendarNotification
            {
                Subject = "Very long meeting subject that exceeds twenty-eight characters definitely",
                Start = futureTime,
                Duration = TimeSpan.FromMinutes(45)
            }
        ]);

        var service = new WindowsTrayService(settingsProvider, appointmentStore, scopeFactory, logger, appLifetime);

        var tooltip = service.GetTooltipText();

        tooltip.Should().Contain("…");
        tooltip.Length.Should().BeLessThanOrEqualTo(63);
    }

    [Fact]
    public void WindowsTrayService_CanBeConstructed_WithAutoStartService()
    {
        var autoStartService = Substitute.For<IAutoStartService>();
        autoStartService.IsEnabled.Returns(true);

        var service = new WindowsTrayService(settingsProvider, appointmentStore, scopeFactory, logger, appLifetime, autoStartService);
        service.Should().NotBeNull();
    }

    [Fact]
    public void IsTaskbarLight_ReturnsBooleanWithoutThrowing()
    {
        var iconManager = new TrayIconManager();
        var action = () => iconManager.IsTaskbarLight();
        action.Should().NotThrow();
    }

    [Fact]
    public void WindowsThemeService_ReturnsValidThemeAndColors_WithoutThrowing()
    {
        var themeService = new WindowsThemeService();

        var isDark = themeService.IsDarkTheme();
        var colors = themeService.GetColors();

        colors.Should().NotBeNull();
        colors.Background.IsEmpty.Should().BeFalse();
        colors.Text.IsEmpty.Should().BeFalse();
        colors.HoverBackground.IsEmpty.Should().BeFalse();
        colors.Border.IsEmpty.Should().BeFalse();
        colors.Separator.IsEmpty.Should().BeFalse();
        colors.Checkmark.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Benchmark_TrayOperations_MeasureExecutionTimes()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var themeService = new WindowsThemeService();
        var t1 = sw.ElapsedMilliseconds;

        var isDark = themeService.IsDarkTheme();
        var t2 = sw.ElapsedMilliseconds;

        var isTaskbarDark = themeService.IsTaskbarDark();
        var t3 = sw.ElapsedMilliseconds;

        var colors = themeService.GetColors();
        var t4 = sw.ElapsedMilliseconds;

        var iconManager = new TrayIconManager(themeService);
        var t5 = sw.ElapsedMilliseconds;

        var icon = iconManager.GetCurrentThemedIcon();
        var t6 = sw.ElapsedMilliseconds;

        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TrayMenuPresenter>.Instance);
        var model = presenter.GetMenuModel();
        var t7 = sw.ElapsedMilliseconds;

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Renderer = new ModernTrayMenuRenderer(colors);
        menu.Items.Add(new System.Windows.Forms.ToolStripMenuItem(model.Title));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Sync"));
        var t8 = sw.ElapsedMilliseconds;

        // Output timings
        t8.Should().BeLessThan(1000);
    }
}
