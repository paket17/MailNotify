using MailNotify.Interfaces;
using MailNotify.Services;

namespace MailNotify.Tests;

public class ExchangeWebServiceTests
{
    [Theory]
    [InlineData("https://exchange.test", "https://exchange.test/EWS/Exchange.asmx")]
    [InlineData("https://exchange.test/", "https://exchange.test/EWS/Exchange.asmx")]
    [InlineData("https://exchange.test/EWS/Exchange.asmx", "https://exchange.test/EWS/Exchange.asmx")]
    public void GetExchangeService_CombinesExchangePath(string mailUrl, string expectedUrl)
    {
        var settings = Substitute.For<ISettingsProvider>();
        settings.MailUrl.Returns(mailUrl);
        var service = new ExchangeWebService(settings).GetExchangeService();

        service.Url.Should().Be(new Uri(expectedUrl));
        service.Credentials.Should().NotBeNull();
    }
}
