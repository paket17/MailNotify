using MailNotify.Entities;
using MailNotify.Services;

namespace MailNotify.Tests;

public class SendUwpNotifyTests
{
    [Fact]
    public void CreateToast_BuildsPersistentMeetingNotificationWithActivationUrl()
    {
        var notification = new CalendarNotification
        {
            Subject = "Planning",
            Location = "Room 42",
            Start = new DateTime(2026, 6, 19, 10, 0, 0),
            Duration = TimeSpan.FromMinutes(45),
            WebUrl = "https://meet.test/planning"
        };

        var xml = SendUwpNotify.CreateToast(notification).GetXml().GetXml();

        xml.Should().Contain("Planning");
        xml.Should().Contain("Room 42");
        xml.Should().Contain("10:00 - 10:45");
        xml.Should().Contain("scenario=\"incomingCall\"");
        xml.Should().Contain("activationType=\"protocol\"");
        xml.Should().Contain("https://meet.test/planning");
    }

    [Fact]
    public void CreateToast_IncludesSummaryMessage_WhenNotificationHasMessage()
    {
        var notification = new CalendarNotification
        {
            Subject = "New appointments for today",
            Message = "10:00 Planning" + Environment.NewLine + "14:00 Review",
            Start = new DateTime(2026, 6, 19, 9, 0, 0),
            Duration = TimeSpan.Zero
        };

        var xml = SendUwpNotify.CreateToast(notification).GetXml().GetXml();

        xml.Should().Contain("New appointments for today");
        xml.Should().Contain("10:00 Planning");
        xml.Should().Contain("14:00 Review");
        xml.Should().Contain("<group><subgroup><text>10:00 Planning</text><text>14:00 Review</text></subgroup></group>");
        xml.Should().NotContain("09:00 - 09:00");
    }

    [Fact]
    public void CreateToast_PutsTimeBeforeLocation_AndOmitsEmptyLocation()
    {
        var withLocation = new CalendarNotification
        {
            Subject = "Daily Standup",
            Location = "Meeting Room 1",
            Start = new DateTime(2026, 6, 19, 10, 0, 0),
            Duration = TimeSpan.FromMinutes(30)
        };
        var withoutLocation = new CalendarNotification
        {
            Subject = "Sync",
            Location = null,
            Start = new DateTime(2026, 6, 19, 11, 0, 0),
            Duration = TimeSpan.FromMinutes(30)
        };

        var xmlWithLoc = SendUwpNotify.CreateToast(withLocation).GetXml().GetXml();
        var xmlWithoutLoc = SendUwpNotify.CreateToast(withoutLocation).GetXml().GetXml();

        var timeIdx = xmlWithLoc.IndexOf("10:00 - 10:30", StringComparison.Ordinal);
        var locIdx = xmlWithLoc.IndexOf("Meeting Room 1", StringComparison.Ordinal);
        timeIdx.Should().BeLessThan(locIdx);

        xmlWithoutLoc.Should().Contain("11:00 - 11:30");
        xmlWithoutLoc.Should().NotContain("<text></text>");
        xmlWithoutLoc.Should().NotContain("<text />");
    }
}
