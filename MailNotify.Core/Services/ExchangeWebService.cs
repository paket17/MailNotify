using MailNotify.Interfaces;
using Microsoft.Exchange.WebServices.Data;
using System.Net;

namespace MailNotify.Services;

public class ExchangeWebService
{
    private const string Exchange = "EWS/Exchange.asmx";
    private readonly ExchangeService exchangeService;

    public ExchangeService GetExchangeService()
    {
        return exchangeService;
    }

    public ExchangeWebService(ISettingsProvider settingsProvider)
    {
        exchangeService = new ExchangeService
        {
            Credentials = CredentialCache.DefaultNetworkCredentials,
            Url = GetExchangeUrl(settingsProvider.MailUrl)
        };
    }

    private static Uri GetExchangeUrl(string mailUrl)
    {
        if (string.IsNullOrWhiteSpace(mailUrl))
            return new Uri("http://localhost");

        if (mailUrl.EndsWith(Exchange, StringComparison.OrdinalIgnoreCase))
            return new Uri(mailUrl);

        var url = mailUrl.EndsWith('/') ? mailUrl + Exchange : mailUrl + "/" + Exchange;
        return new Uri(url);
    }
}

