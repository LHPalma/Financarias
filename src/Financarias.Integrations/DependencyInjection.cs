using System.Net.Http.Headers;
using Financarias.Application.Addresses;
using Financarias.Application.Holidays.Import;
using Financarias.Application.MarketData.Cryptos.Gateways;
using Financarias.Application.MarketData.ForeignExchange.Gateways;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Application.MarketData.Stocks.Gateways;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Application.News;
using Financarias.Integrations.Addresses.ViaCep.Clients;
using Financarias.Integrations.Addresses.ViaCep.Providers;
using Financarias.Integrations.Anbima.Holidays.Clients;
using Financarias.Integrations.Anbima.Holidays.Providers;
using Financarias.Integrations.BrasilApi.Holidays.Clients;
using Financarias.Integrations.BrasilApi.Holidays.Providers;
using Financarias.Integrations.MarketData.Anp.Fuel.Clients;
using Financarias.Integrations.MarketData.Anp.Fuel.Providers;
using Financarias.Integrations.MarketData.B3.CorporateEvents.Clients;
using Financarias.Integrations.MarketData.B3.CorporateEvents.Providers;
using Financarias.Integrations.MarketData.B3.Cotahist.Clients;
using Financarias.Integrations.MarketData.B3.Cotahist.Providers;
using Financarias.Application.MarketData.Issuers.Import;
using Financarias.Integrations.MarketData.B3.Indexes.Clients;
using Financarias.Integrations.MarketData.B3.Issuers.Clients;
using Financarias.Integrations.MarketData.B3.Issuers.Providers;
using Financarias.Integrations.MarketData.B3.Indexes.Providers;
using Financarias.Integrations.MarketData.Bcb.Sgs.Clients;
using Financarias.Integrations.MarketData;
using Financarias.Integrations.MarketData.Bcb.Sgs.Providers;
using Financarias.Integrations.MarketData.Cboe.Clients;
using Financarias.Integrations.MarketData.Cboe.Providers;
using Financarias.Integrations.MarketData.Eia.Clients;
using Financarias.Integrations.MarketData.Eia.Providers;
using Financarias.Integrations.MarketData.Fed.Clients;
using Financarias.Integrations.MarketData.Fed.Providers;
using Financarias.Integrations.MarketData.WorldBank.Clients;
using Financarias.Integrations.MarketData.WorldBank.Providers;
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

        var brasilApiBaseUrl = configuration["Integrations:BrasilApi:BaseUrl"]
                               ?? throw new InvalidOperationException("Integrations:BrasilApi:BaseUrl not configured.");

        services
            .AddRefitClient<IBrasilApiClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(brasilApiBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(1);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<IHolidayProvider, BrasilApiHolidayProvider>();

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

        var anpBaseUrl = configuration["Integrations:Anp:BaseUrl"]
                         ?? throw new InvalidOperationException("Integrations:Anp:BaseUrl not configured.");

        services
            .AddRefitClient<IAnpFuelClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(anpBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<IFuelPriceProvider, AnpFuelProvider>();

        var b3BaseUrl = configuration["Integrations:B3:BaseUrl"]
                        ?? throw new InvalidOperationException("Integrations:B3:BaseUrl not configured.");

        services
            .AddRefitClient<IB3CotahistClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(b3BaseUrl);
                client.Timeout = TimeSpan.FromMinutes(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<IDailyQuoteProvider, B3CotahistProvider>();

        var b3IndexesBaseUrl = configuration["Integrations:B3Indexes:BaseUrl"]
                               ?? throw new InvalidOperationException("Integrations:B3Indexes:BaseUrl not configured.");

        services
            .AddRefitClient<IB3IndexClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(b3IndexesBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(2);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<IIndexQuoteProvider, B3IndexQuoteProvider>();

        var bcbBaseUrl = configuration["Integrations:Bcb:BaseUrl"]
                         ?? throw new InvalidOperationException("Integrations:Bcb:BaseUrl not configured.");

        services
            .AddRefitClient<IBcbSgsClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(bcbBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(1);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<BcbIndicatorValueProvider>();

        AddExternalIndicatorClient<IEiaClient>(services, configuration, "Integrations:Eia:BaseUrl");
        AddExternalIndicatorClient<ICboeClient>(services, configuration, "Integrations:Cboe:BaseUrl");
        AddExternalIndicatorClient<IFedDataDownloadClient>(services, configuration, "Integrations:Fed:BaseUrl");
        AddExternalIndicatorClient<IWorldBankPageClient>(services, configuration, "Integrations:WorldBank:PageBaseUrl");
        AddExternalIndicatorClient<IWorldBankDocsClient>(services, configuration, "Integrations:WorldBank:DocsBaseUrl");

        services.AddScoped<EiaIndicatorValueProvider>();
        services.AddScoped<CboeIndicatorValueProvider>();
        services.AddScoped<FedDollarIndexValueProvider>();
        services.AddScoped<WorldBankIndicatorValueProvider>();
        services.AddScoped<IIndicatorValueProvider, IndicatorValueProviderRouter>();

        var b3CompaniesBaseUrl = configuration["Integrations:B3Companies:BaseUrl"]
                                 ?? throw new InvalidOperationException("Integrations:B3Companies:BaseUrl not configured.");

        services
            .AddRefitClient<IB3CompanyClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(b3CompaniesBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(1);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<ICorporateEventProvider, B3CorporateEventProvider>();

        services
            .AddRefitClient<IB3IssuerClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(b3CompaniesBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(1);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });

        services.AddScoped<IIssuerProvider, B3IssuerProvider>();

        return services;
    }

    private static void AddExternalIndicatorClient<TClient>(
        IServiceCollection services,
        IConfiguration configuration,
        string baseUrlKey)
        where TClient : class
    {
        var baseUrl = configuration[baseUrlKey]
                      ?? throw new InvalidOperationException($"{baseUrlKey} not configured.");

        services
            .AddRefitClient<TClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromMinutes(2);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Financarias/1.0");
            });
    }
}