using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class ExchangeWebServiceTests
{
    [Fact]
    public void GetExchangeService_ReturnsServiceConfiguredFromSettings()
    {
        var settings = Substitute.For<ISettingsProvider>();
        settings.MailUrl.Returns("https://exchange.test/EWS/Exchange.asmx");
        var service = new ExchangeWebService(settings).GetExchangeService();

        service.Url.Should().Be(new Uri(settings.MailUrl));
        service.Credentials.Should().NotBeNull();
    }
}
