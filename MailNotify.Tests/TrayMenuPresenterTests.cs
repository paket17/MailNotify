using MailNotify.Entities;
using MailNotify.Interfaces;
using MailNotify.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MailNotify.Tests;

public class TrayMenuPresenterTests
{
    private readonly AppointmentStore appointmentStore = new();
    private readonly ISettingsProvider settingsProvider = Substitute.For<ISettingsProvider>();
    private readonly IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ILogger<TrayMenuPresenter> logger = Substitute.For<ILogger<TrayMenuPresenter>>();
    private readonly IAutoStartService autoStartService = Substitute.For<IAutoStartService>();
    private readonly IHostApplicationLifetime appLifetime = Substitute.For<IHostApplicationLifetime>();

    private class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    [Fact]
    public void GetMenuModel_ReturnsDefaultTitle_AndEmptyMeetings_WhenNoAppointments()
    {
        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime);

        var model = presenter.GetMenuModel();

        model.Title.Should().Be("MailNotify");
        model.UpcomingMeetings.Should().BeEmpty();
        model.HasUpcomingMeetings.Should().BeFalse();
    }

    [Fact]
    public void GetMenuModel_PopulatesUpcomingAndOngoingMeetings_AndExcludesPastMeetings()
    {
        var fixedTime = new DateTime(2026, 10, 16, 14, 0, 0);
        var timeProvider = new TestTimeProvider(new DateTimeOffset(fixedTime, TimeSpan.Zero));

        appointmentStore.SetAppointments([
            // Past meeting (ended before 14:00)
            new CalendarNotification
            {
                Subject = "Past Meeting",
                Start = new DateTime(2026, 10, 16, 12, 0, 0),
                Duration = TimeSpan.FromMinutes(30)
            },
            // Ongoing meeting (started 13:45, ends 14:15)
            new CalendarNotification
            {
                Subject = "Ongoing Meeting",
                Start = new DateTime(2026, 10, 16, 13, 45, 0),
                Duration = TimeSpan.FromMinutes(30),
                WebUrl = "https://teams.microsoft.com/meet/123"
            },
            // Future meeting (15:00 - 15:30)
            new CalendarNotification
            {
                Subject = "Future Meeting",
                Start = new DateTime(2026, 10, 16, 15, 0, 0),
                Duration = TimeSpan.FromMinutes(30),
                WebUrl = "https://teams.microsoft.com/meet/456"
            }
        ]);

        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime,
            timeProvider);

        var model = presenter.GetMenuModel();

        model.HasUpcomingMeetings.Should().BeTrue();
        model.UpcomingMeetings.Should().HaveCount(2);

        var ongoing = model.UpcomingMeetings[0];
        ongoing.Subject.Should().Be("Ongoing Meeting");
        ongoing.IsOngoing.Should().BeTrue();
        ongoing.TimeRange.Should().Be("Now - 14:15");
        ongoing.DisplayText.Should().Be("Now - 14:15   Ongoing Meeting");
        ongoing.HasUrl.Should().BeTrue();
        ongoing.WebUrl.Should().Be("https://teams.microsoft.com/meet/123");

        var future = model.UpcomingMeetings[1];
        future.Subject.Should().Be("Future Meeting");
        future.IsOngoing.Should().BeFalse();
        future.TimeRange.Should().Be("15:00 - 15:30");
        future.DisplayText.Should().Be("15:00 - 15:30   Future Meeting");
        future.HasUrl.Should().BeTrue();
    }

    [Fact]
    public void GetMenuModel_LimitsMeetingsToFive()
    {
        var fixedTime = new DateTime(2026, 10, 16, 10, 0, 0);
        var timeProvider = new TestTimeProvider(new DateTimeOffset(fixedTime, TimeSpan.Zero));

        var meetings = Enumerable.Range(1, 8).Select(i => new CalendarNotification
        {
            Subject = $"Meeting {i}",
            Start = fixedTime.AddHours(i),
            Duration = TimeSpan.FromMinutes(30)
        }).ToList();

        appointmentStore.SetAppointments(meetings);

        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime,
            timeProvider);

        var model = presenter.GetMenuModel();

        model.UpcomingMeetings.Should().HaveCount(5);
        model.UpcomingMeetings.Select(m => m.Subject).Should().Equal(
            "Meeting 1", "Meeting 2", "Meeting 3", "Meeting 4", "Meeting 5");
    }

    [Fact]
    public void GetMenuModel_SetsWebCalendarUrl_AndAutoStartProperties()
    {
        settingsProvider.MailUrl.Returns("https://mail.company.com");
        autoStartService.IsEnabled.Returns(true);

        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime);

        var model = presenter.GetMenuModel();

        model.WebCalendarUrl.Should().Be("https://mail.company.com");
        model.HasWebCalendarUrl.Should().BeTrue();
        model.IsAutoStartSupported.Should().BeTrue();
        model.IsAutoStartEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://mail.company.com", "https://mail.company.com/owa/#path=/calendar", "https://mail.company.com/owa/#path=/calendar")]
    [InlineData("https://mail.company.com", "owa/#path=/calendar", "https://mail.company.com/owa/#path=/calendar")]
    [InlineData("https://mail.company.com", "/owa/#path=/calendar", "https://mail.company.com/owa/#path=/calendar")]
    [InlineData("https://mail.company.com/", "owa/#path=/calendar", "https://mail.company.com/owa/#path=/calendar")]
    [InlineData("https://mail.company.com/", "/owa/#path=/calendar", "https://mail.company.com/owa/#path=/calendar")]
    [InlineData("https://mail.company.com", "///owa/#path=/calendar", "https://mail.company.com/owa/#path=/calendar")]
    [InlineData("https://mail.company.com", "", "https://mail.company.com")]
    [InlineData("https://mail.company.com", "   ", "https://mail.company.com")]
    [InlineData("https://mail.company.com", null, "https://mail.company.com")]
    [InlineData("https://mail.company.com", "https://custom.calendar.com", "https://custom.calendar.com")]
    public void BuildCalendarUrl_CombinesUrlAndPathCorrectly(string? mailUrl, string? calendarUrl, string expected)
    {
        var result = TrayMenuPresenter.BuildCalendarUrl(mailUrl, calendarUrl);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetMenuModel_UsesCalendarUrl_WhenConfigured()
    {
        var traySettings = Substitute.For<ITraySettingsProvider>();
        traySettings.CalendarUrl.Returns("https://mail.company.com/owa/#path=/calendar");
        settingsProvider.MailUrl.Returns("https://mail.company.com");

        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime,
            traySettingsProvider: traySettings);

        var model = presenter.GetMenuModel();

        model.WebCalendarUrl.Should().Be("https://mail.company.com/owa/#path=/calendar");
        model.HasWebCalendarUrl.Should().BeTrue();
    }

    [Fact]
    public void GetMenuModel_FallsBackToMailUrl_WhenCalendarUrlIsEmpty()
    {
        var traySettings = Substitute.For<ITraySettingsProvider>();
        traySettings.CalendarUrl.Returns(string.Empty);
        settingsProvider.MailUrl.Returns("https://mail.company.com");

        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime,
            traySettingsProvider: traySettings);

        var model = presenter.GetMenuModel();

        model.WebCalendarUrl.Should().Be("https://mail.company.com");
        model.HasWebCalendarUrl.Should().BeTrue();
    }

    [Fact]
    public void TraySettingsProvider_ReturnsConfiguredCalendarUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CalendarUrl"] = "https://mail.company.com/owa/#path=/calendar"
            })
            .Build();

        var provider = new TraySettingsProvider(configuration);

        provider.CalendarUrl.Should().Be("https://mail.company.com/owa/#path=/calendar");
    }

    [Fact]
    public void TraySettingsProvider_FallsBackToCalendarPath_WhenCalendarUrlNotSet()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CalendarPath"] = "https://mail.company.com/owa/#path=/calendar"
            })
            .Build();

        var provider = new TraySettingsProvider(configuration);

        provider.CalendarUrl.Should().Be("https://mail.company.com/owa/#path=/calendar");
    }

    [Fact]
    public void TraySettingsProvider_ReturnsEmptyString_WhenNotConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var provider = new TraySettingsProvider(configuration);

        provider.CalendarUrl.Should().BeEmpty();
    }

    [Fact]
    public void GetMenuModel_WhenAutoStartServiceIsNull_SetsSupportedFalse()
    {
        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService: null,
            appLifetime);

        var model = presenter.GetMenuModel();

        model.IsAutoStartSupported.Should().BeFalse();
        model.IsAutoStartEnabled.Should().BeFalse();
    }

    [Fact]
    public void GetTooltipText_ReturnsNoUpcomingMeetings_WhenNoUpcoming()
    {
        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime);

        var tooltip = presenter.GetTooltipText();

        tooltip.Should().Be("MailNotify\nNo upcoming meetings");
    }

    [Fact]
    public void GetTooltipText_ReturnsFormattedNextMeeting_AndTruncatesSubject()
    {
        var fixedTime = new DateTime(2026, 10, 16, 12, 0, 0);
        var timeProvider = new TestTimeProvider(new DateTimeOffset(fixedTime, TimeSpan.Zero));

        appointmentStore.SetAppointments([
            new CalendarNotification
            {
                Subject = "Architecture and Engineering Sync Discussion",
                Start = fixedTime.AddMinutes(45),
                Duration = TimeSpan.FromMinutes(30)
            }
        ]);

        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime,
            timeProvider);

        var tooltip = presenter.GetTooltipText();

        tooltip.Should().Be("MailNotify\nNext: 12:45 (Architecture and Engineerin…)");
        tooltip.Length.Should().BeLessThanOrEqualTo(63);
    }

    [Fact]
    public void GetTooltipText_ReturnsFormattedOngoingMeeting_WhenMeetingIsOngoing()
    {
        var fixedTime = new DateTime(2026, 10, 16, 12, 0, 0);
        var timeProvider = new TestTimeProvider(new DateTimeOffset(fixedTime, TimeSpan.Zero));

        appointmentStore.SetAppointments([
            new CalendarNotification
            {
                Subject = "Architecture and Engineering Sync Discussion",
                Start = fixedTime.AddMinutes(-15),
                Duration = TimeSpan.FromMinutes(45) // ends at 12:30
            }
        ]);

        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime,
            timeProvider);

        var tooltip = presenter.GetTooltipText();

        tooltip.Should().Be("MailNotify\nNow: 12:30 (Architecture and Engine…)");
        tooltip.Length.Should().BeLessThanOrEqualTo(63);
    }

    [Fact]
    public void ToggleAutoStart_CallsSetEnabled_OnAutoStartService()
    {
        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime);

        presenter.ToggleAutoStart(true);
        autoStartService.Received(1).SetEnabled(true);

        presenter.ToggleAutoStart(false);
        autoStartService.Received(1).SetEnabled(false);
    }

    [Fact]
    public void ExitApplication_CallsStopApplication_OnAppLifetime()
    {
        var presenter = new TrayMenuPresenter(
            appointmentStore,
            settingsProvider,
            scopeFactory,
            logger,
            autoStartService,
            appLifetime);

        presenter.ExitApplication();

        appLifetime.Received(1).StopApplication();
    }

    [Theory]
    [InlineData("Short text", 20, false, "Short text")]
    [InlineData("Very long text to truncate", 10, false, "Very long…")]
    [InlineData("Text with\r\nline breaks", 30, false, "Text with line breaks")]
    [InlineData("Text with\r\nline breaks", 30, true, "Text with\r\nline breaks")]
    [InlineData(null, 10, false, "")]
    [InlineData("", 10, false, "")]
    public void Truncate_BehavesCorrectly(string? text, int max, bool preserveNewLines, string expected)
    {
        TrayMenuPresenter.Truncate(text, max, preserveNewLines).Should().Be(expected);
    }
}
