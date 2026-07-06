using System.Net.Http.Headers;
using Financarias.Application.Addresses;
using Financarias.Application.Holidays.Import;
using Financarias.Application.MarketData.Cryptos.Gateways;
using Financarias.Application.MarketData.ForeignExchange.Gateways;
using Financarias.Application.MarketData.Stocks.Gateways;
using Financarias.Application.News;
using Financarias.Integrations.Addresses.ViaCep.Clients;
using Financarias.Integrations.Addresses.ViaCep.Providers;
using Financarias.Integrations.Anbima.Holidays.Clients;
using Financarias.Integrations.Anbima.Holidays.Providers;
using Financarias.Integrations.MarketData.Brapi.Clients;
using Financarias.Integrations.MarketData.Brapi.Providers;
using Financarias.Integrations.MarketData.CoinGecko.Clients;
using Financarias.Integrations.MarketData.CoinGecko.Providers;
using Financarias.Integrations.MarketData.ErApi.Clients;
using Financarias.Integrations.MarketData.ErApi.Providers;
using Financarias.Integrations.News.InfoMoney.Clients;
using Financarias.Integrations.News.InfoMoney.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Financarias.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        var viaCepBaseUrl = configuration["Integrations:ViaCep:BaseUrl"]
                            ?? throw new InvalidOperationException("Integrations:ViaCep:BaseUrl not configured.");

        services
            .AddRefitClient<IViaCepClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(viaCepBaseUrl));

        services.AddScoped<IAddressLookupProvider, ViaCepProvider>();

        var anbimaBaseUrl = configuration["Integrations:Anbima:BaseUrl"]
                            ?? throw new InvalidOperationException("Integrations:Anbima:BaseUrl not configured.");

        services
            .AddRefitClient<IAnbimaHolidayClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(anbimaBaseUrl));

        services.AddScoped<IHolidayProvider, AnbimaHolidayProvider>();

        var brapiBaseUrl = configuration["Integrations:Brapi:BaseUrl"]
                           ?? throw new InvalidOperationException("Integrations:Brapi:BaseUrl not configured.");
        var brapiToken = configuration["Integrations:Brapi:Token"]
                         ?? throw new InvalidOperationException("Integrations:Brapi:Token not configured.");

        services
            .AddRefitClient<IBrapiClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(brapiBaseUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", brapiToken);
            });

        services.AddScoped<IStockQuoteGateway, BrapiQuoteProvider>();

        var coinGeckoBaseUrl = configuration["Integrations:CoinGecko:BaseUrl"]
                               ?? throw new InvalidOperationException("Integrations:CoinGecko:BaseUrl not configured.");

        services
            .AddRefitClient<ICoinGeckoClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(coinGeckoBaseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<ICryptoQuoteGateway, CoinGeckoQuoteProvider>();

        var infoMoneyBaseUrl = configuration["Integrations:InfoMoney:BaseUrl"]
                               ?? throw new InvalidOperationException("Integrations:InfoMoney:BaseUrl not configured.");

        services
            .AddRefitClient<IInfoMoneyClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(infoMoneyBaseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<INewsFeedGateway, InfoMoneyNewsProvider>();

        var erApiBaseUrl = configuration["Integrations:ErApi:BaseUrl"]
                           ?? throw new InvalidOperationException("Integrations:ErApi:BaseUrl not configured.");

        services
            .AddRefitClient<IErApiClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(erApiBaseUrl));

        services.AddScoped<IFxRateGateway, ErApiRateProvider>();

        return services;
    }
}