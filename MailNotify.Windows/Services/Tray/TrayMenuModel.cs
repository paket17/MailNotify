namespace MailNotify.Services;

public record TrayMeetingItem(
    string Subject,
    string TimeRange,
    string DisplayText,
    string? WebUrl,
    bool IsOngoing,
    bool HasUrl);

public class TrayMenuModel
{
    public string Title { get; init; } = "MailNotify";
    public IReadOnlyList<TrayMeetingItem> UpcomingMeetings { get; init; } = [];
    public bool HasUpcomingMeetings => UpcomingMeetings.Count > 0;
    public string? WebCalendarUrl { get; init; }
    public bool HasWebCalendarUrl => !string.IsNullOrWhiteSpace(WebCalendarUrl);
    public bool IsAutoStartSupported { get; init; }
    public bool IsAutoStartEnabled { get; init; }
}
