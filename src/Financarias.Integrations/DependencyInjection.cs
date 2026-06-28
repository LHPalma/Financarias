using Financarias.Application.Addresses;
using Financarias.Application.Holidays.Import;
using Financarias.Integrations.Addresses.ViaCep.Clients;
using Financarias.Integrations.Addresses.ViaCep.Providers;
using Financarias.Integrations.Anbima.Holidays;
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
        
        services.Configure<AnbimaOptions>(configuration.GetSection(AnbimaOptions.SectionName));
        services.AddScoped<IHolidayProvider, AnbimaHolidayProvider>();

        return services;
    }
}