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
}
