using MailNotify.Services;
using Microsoft.Extensions.Configuration;

namespace MailNotify.Tests;

public class SettingsProviderTests
{
    [Fact]
    public void Properties_ReturnDefaults_WhenConfigurationValuesAreMissing()
    {
        var provider = new SettingsProvider(CreateConfiguration([]));

        provider.ReminderOffsetMinutes.Should().Be(15);
        provider.UpdateOffsetMinutes.Should().Be(3);
        provider.MailUrl.Should().BeEmpty();
        provider.AutoStart.Should().BeTrue();
        provider.NotifyDailyAppointments.Should().BeTrue();
    }

    [Fact]
    public void Properties_ReturnConfiguredValues()
    {
        var provider = new SettingsProvider(CreateConfiguration(new Dictionary<string, string?>
        {
            ["ReminderOffsetMinutes"] = "7.5",
            ["UpdateOffsetMinutes"] = "2",
            ["MailUrl"] = "https://exchange.test",
            ["AutoStart"] = "false",
            ["NotifyDailyAppointments"] = "false"
        }));

        provider.ReminderOffsetMinutes.Should().Be(7.5);
        provider.UpdateOffsetMinutes.Should().Be(2);
        provider.MailUrl.Should().Be("https://exchange.test/EWS/Exchange.asmx");
        provider.AutoStart.Should().BeFalse();
        provider.NotifyDailyAppointments.Should().BeFalse();
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
