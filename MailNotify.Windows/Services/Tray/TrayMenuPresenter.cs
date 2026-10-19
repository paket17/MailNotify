using MailNotify.Interfaces;
using System.Diagnostics;

namespace MailNotify.Services;

public class TrayMenuPresenter(
    AppointmentStore appointmentStore,
    ISettingsProvider settingsProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<TrayMenuPresenter> logger,
    IAutoStartService? autoStartService = null,
    IHostApplicationLifetime? appLifetime = null,
    TimeProvider? timeProvider = null,
    ITraySettingsProvider? traySettingsProvider = null) : ITrayMenuPresenter
{
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

    public TrayMenuModel GetMenuModel()
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var upcoming = appointmentStore.GetAppointments()
            .Where(a => a.Start > now || (a.Start <= now && a.Start + a.Duration > now))
            .OrderBy(a => a.Start)
            .Take(5)
            .Select(meeting =>
            {
                var isOngoing = meeting.Start <= now;
                var timeRange = isOngoing
                    ? $"Now - {meeting.Start + meeting.Duration:HH:mm}"
                    : $"{meeting.Start:HH:mm} - {meeting.Start + meeting.Duration:HH:mm}";
                var displayText = $"{timeRange}   {Truncate(meeting.Subject, 30)}";
                var hasUrl = !string.IsNullOrWhiteSpace(meeting.WebUrl);

                return new TrayMeetingItem(
                    meeting.Subject ?? string.Empty,
                    timeRange,
                    displayText,
                    meeting.WebUrl,
                    isOngoing,
                    hasUrl);
            })
            .ToList();

        var calendarUrl = traySettingsProvider?.CalendarUrl;
        var webCalendarUrl = BuildCalendarUrl(settingsProvider.MailUrl, calendarUrl);

        return new TrayMenuModel
        {
            Title = "MailNotify",
            UpcomingMeetings = upcoming,
            WebCalendarUrl = webCalendarUrl,
            IsAutoStartSupported = autoStartService is not null,
            IsAutoStartEnabled = autoStartService?.IsEnabled ?? false
        };
    }

    public string GetTooltipText()
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var appointments = appointmentStore.GetAppointments();

        var ongoing = appointments
            .Where(a => a.Start <= now && a.Start + a.Duration > now)
            .OrderBy(a => a.Start)
            .FirstOrDefault();

        if (ongoing is not null)
        {
            var endTime = ongoing.Start + ongoing.Duration;
            var text = $"MailNotify\nNow: {endTime:HH:mm} ({Truncate(ongoing.Subject, 24)})";
            return Truncate(text, 63, preserveNewLines: true);
        }

        var next = appointments
            .Where(a => a.Start > now)
            .OrderBy(a => a.Start)
            .FirstOrDefault();

        var tooltipText = next is not null
            ? $"MailNotify\nNext: {next.Start:HH:mm} ({Truncate(next.Subject, 28)})"
            : "MailNotify\nNo upcoming meetings";

        return Truncate(tooltipText, 63, preserveNewLines: true);
    }

    public void TriggerSync()
    {
        Task.Run(() =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notifyWorker = scope.ServiceProvider.GetRequiredService<NotifyWorker>();
                notifyWorker.GetAppointments(CancellationToken.None);
                notifyWorker.CheckReminders(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run manual sync from tray");
            }
        });
    }

    public void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open URL {Url}", url);
        }
    }

    public void ToggleAutoStart(bool enabled)
    {
        autoStartService?.SetEnabled(enabled);
    }

    public void ExitApplication()
    {
        appLifetime?.StopApplication();
    }

    public static string Truncate(string? text, int maxLength, bool preserveNewLines = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = preserveNewLines
            ? text.Trim()
            : text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();

        if (cleaned.Length > maxLength)
            return cleaned[..(maxLength - 1)] + "…";

        return cleaned;
    }

    public static string BuildCalendarUrl(string? mailUrl, string? calendarUrl)
    {
        if (string.IsNullOrWhiteSpace(calendarUrl))
            return mailUrl?.Trim() ?? string.Empty;

        var trimmed = calendarUrl.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var cleanPath = trimmed.TrimStart('/');
        if (string.IsNullOrWhiteSpace(mailUrl))
            return cleanPath;

        var baseUrl = mailUrl.Trim().TrimEnd('/');
        return $"{baseUrl}/{cleanPath}";
    }
}
