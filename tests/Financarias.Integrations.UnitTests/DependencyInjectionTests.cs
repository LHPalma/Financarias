using Financarias.Integrations.Addresses.ViaCep.Clients;
using Financarias.Integrations.Anbima.Holidays.Clients;
using Financarias.Integrations.MarketData.Anp.Fuel.Clients;
using Financarias.Integrations.MarketData.Brapi.Clients;
using Financarias.Integrations.MarketData.CoinGecko.Clients;
using Financarias.Integrations.MarketData.ErApi.Clients;
using Financarias.Integrations.News.InfoMoney.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Integrations.UnitTests;

public class DependencyInjectionTests
{
    public static TheoryData<Type> RefitClients =>
    [
        typeof(IViaCepClient),
        typeof(IAnbimaHolidayClient),
        typeof(IBrapiClient),
        typeof(ICoinGeckoClient),
        typeof(IInfoMoneyClient),
        typeof(IErApiClient),
        typeof(IAnpFuelClient)
    ];

    [Theory(DisplayName = "Resolve cada cliente Refit registrado sem chamar a rede")]
    [MemberData(nameof(RefitClients))]
    public void AddIntegrations_ResolvesRefitClient_WhenConfigured(Type clientType)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integrations:ViaCep:BaseUrl"] = "https://viacep.com.br/ws",
                ["Integrations:Anbima:BaseUrl"] = "https://www.anbima.com.br",
                ["Integrations:Brapi:BaseUrl"] = "https://brapi.dev",
                ["Integrations:Brapi:Token"] = "test-token",
                ["Integrations:CoinGecko:BaseUrl"] = "https://api.coingecko.com/api/v3",
                ["Integrations:InfoMoney:BaseUrl"] = "https://www.infomoney.com.br",
                ["Integrations:ErApi:BaseUrl"] = "https://open.er-api.com",
                ["Integrations:Anp:BaseUrl"] = "https://www.gov.br/anp"
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddIntegrations(configuration)
            .BuildServiceProvider();

        // Act
        var client = provider.GetService(clientType);

        // Assert
        Assert.NotNull(client);
    }
}
