using System.Net.Http.Headers;
using Financarias.Application.Addresses;
using Financarias.Application.Holidays.Import;
using Financarias.Application.MarketData.Stocks.Gateways;
using Financarias.Integrations.Addresses.ViaCep.Clients;
using Financarias.Integrations.Addresses.ViaCep.Providers;
using Financarias.Integrations.Anbima.Holidays.Clients;
using Financarias.Integrations.Anbima.Holidays.Providers;
using Financarias.Integrations.MarketData.Brapi.Clients;
using Financarias.Integrations.MarketData.Brapi.Providers;
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

        return services;
    }
}