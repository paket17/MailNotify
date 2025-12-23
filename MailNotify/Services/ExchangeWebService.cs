using Microsoft.Exchange.WebServices.Data;
using System.Net;

namespace MailNotify.Services;

public class ExchangeWebService
{
    private readonly ExchangeService exchangeService;
    public ExchangeService GetExchangeService()
    {
        return exchangeService;
    }

    public ExchangeWebService(SettingsProvider settingsProvider)
    {
        exchangeService = new ExchangeService
        {
            Credentials = CredentialCache.DefaultNetworkCredentials,
            Url = new Uri(settingsProvider.ExchangeUrl)
        };
    }
}

