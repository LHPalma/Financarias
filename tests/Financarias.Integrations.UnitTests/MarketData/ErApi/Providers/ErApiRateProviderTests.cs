using Financarias.Domain.MarketData;
using Financarias.Integrations.MarketData.ErApi.Clients;
using Financarias.Integrations.MarketData.ErApi.DTOs.Responses;
using Financarias.Integrations.MarketData.ErApi.Providers;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData.ErApi.Providers;

public class ErApiRateProviderTests
{
    private readonly IErApiClient _client = Substitute.For<IErApiClient>();

    [Fact(DisplayName = "Mapeia só as moedas do enum e converte o timestamp unix")]
    public async Task GetLatestRates_MapsKnownCurrenciesAndTimestamp()
    {
        // Arrange
        _client.GetLatestAsync("BRL", Arg.Any<CancellationToken>())
            .Returns(new ErApiLatestResponse(
                Result: "success",
                BaseCode: "BRL",
                TimeLastUpdateUnix: 1_751_673_600,
                Rates: new Dictionary<string, decimal>
                {
                    ["BRL"] = 1m,
                    ["USD"] = 0.20m,
                    ["EUR"] = 0.18m,
                    ["XAU"] = 0.0001m // moeda fora do enum -> ignorada
                }));
        var provider = new ErApiRateProvider(_client);

        // Act
        var snapshot = await provider.GetLatestRatesAsync(Currency.Brl);

        // Assert
        Assert.Equal(Currency.Brl, snapshot.Base);
        Assert.Equal(0.20m, snapshot.Rates[Currency.Usd]);
        Assert.Equal(0.18m, snapshot.Rates[Currency.Eur]);
        Assert.False(snapshot.Rates.ContainsKey(Currency.Gbp)); // não veio na resposta
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_751_673_600), snapshot.AsOf);
    }
}
