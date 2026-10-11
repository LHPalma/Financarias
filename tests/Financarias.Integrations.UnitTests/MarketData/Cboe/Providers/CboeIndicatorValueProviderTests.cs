using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Cboe.Clients;
using Financarias.Integrations.MarketData.Cboe.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData.Cboe.Providers;

public class CboeIndicatorValueProviderTests
{
    private readonly ICboeClient _client = Substitute.For<ICboeClient>();
    private readonly CboeIndicatorValueProvider _provider;

    public CboeIndicatorValueProviderTests()
    {
        _provider = new CboeIndicatorValueProvider(_client, NullLogger<CboeIndicatorValueProvider>.Instance);
    }

    [Fact(DisplayName = "GetYearAsync lê o fechamento com data MM/dd/yyyy, só do ano pedido e em ordem de data")]
    public async Task GetYear_ReadsTheClose_ForTheRequestedYear()
    {
        // Arrange
        _client.GetVixHistoryAsync(default).ReturnsForAnyArgs(
            "DATE,OPEN,HIGH,LOW,CLOSE\n12/31/2025,14.0,14.5,13.9,14.2\n01/06/2026,15.1,16.0,15.0,15.8\n01/05/2026,16.0,16.5,15.5,16.234567891\n");

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.Vix, 2026);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(new DateOnly(2026, 1, 5), items[0].ReferenceDate);
        Assert.Equal(16.234568m, items[0].Value);
        Assert.Equal(15.8m, items[1].Value);
        Assert.All(items, item => Assert.Equal(EconomicIndicator.Vix, item.Indicator));
    }

    [Fact(DisplayName = "GetYearAsync descarta linha com data ou fechamento inválido")]
    public async Task GetYear_DiscardsUnusableLines()
    {
        // Arrange
        _client.GetVixHistoryAsync(default).ReturnsForAnyArgs(
            "DATE,OPEN,HIGH,LOW,CLOSE\nnot-a-date,1,1,1,1\n01/05/2026,1,1,1,abc\n01/06/2026,1,1\n01/07/2026,1,1,1,17.5\n");

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.Vix, 2026);

        // Assert
        Assert.Equal(17.5m, Assert.Single(items).Value);
    }

    [Fact(DisplayName = "GetYearAsync recusa um indicador que não é da Cboe")]
    public async Task GetYear_RejectsAnIndicatorFromAnotherSource()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _provider.GetYearAsync(EconomicIndicator.BrentOil, 2026));
    }
}
