using Financarias.Integrations.Addresses.ViaCep.Clients;
using Financarias.Integrations.Anbima.Holidays.Clients;
using Financarias.Integrations.BrasilApi.Holidays.Clients;
using Financarias.Integrations.MarketData.Anp.Fuel.Clients;
using Financarias.Integrations.MarketData.B3.CorporateEvents.Clients;
using Financarias.Integrations.MarketData.B3.Cotahist.Clients;
using Financarias.Integrations.MarketData.B3.Indexes.Clients;
using Financarias.Integrations.MarketData.Bcb.Sgs.Clients;
using Financarias.Integrations.MarketData.Cboe.Clients;
using Financarias.Integrations.MarketData.Eia.Clients;
using Financarias.Integrations.MarketData.Fed.Clients;
using Financarias.Integrations.MarketData.WorldBank.Clients;
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
        typeof(IAnpFuelClient),
        typeof(IB3CotahistClient),
        typeof(IB3IndexClient),
        typeof(IBcbSgsClient),
        typeof(IEiaClient),
        typeof(ICboeClient),
        typeof(IFedDataDownloadClient),
        typeof(IWorldBankPageClient),
        typeof(IWorldBankDocsClient),
        typeof(IB3CompanyClient),
        typeof(IBrasilApiClient)
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
                ["Integrations:Anp:BaseUrl"] = "https://www.gov.br/anp",
                ["Integrations:B3:BaseUrl"] = "https://bvmf.bmfbovespa.com.br",
                ["Integrations:B3Indexes:BaseUrl"] = "https://sistemaswebb3-listados.b3.com.br",
                ["Integrations:Bcb:BaseUrl"] = "https://api.bcb.gov.br",
                ["Integrations:Eia:BaseUrl"] = "https://www.eia.gov",
                ["Integrations:Cboe:BaseUrl"] = "https://cdn.cboe.com",
                ["Integrations:Fed:BaseUrl"] = "https://www.federalreserve.gov",
                ["Integrations:WorldBank:PageBaseUrl"] = "https://www.worldbank.org",
                ["Integrations:WorldBank:DocsBaseUrl"] = "https://thedocs.worldbank.org",
                ["Integrations:B3Companies:BaseUrl"] = "https://sistemaswebb3-listados.b3.com.br",
                ["Integrations:BrasilApi:BaseUrl"] = "https://brasilapi.com.br"
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
