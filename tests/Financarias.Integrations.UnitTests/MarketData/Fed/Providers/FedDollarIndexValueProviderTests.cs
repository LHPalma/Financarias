using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Fed.Clients;
using Financarias.Integrations.MarketData.Fed.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData.Fed.Providers;

public class FedDollarIndexValueProviderTests
{
    private const string Header =
        "\"Series Description\",\"Nominal Broad Dollar Index\"\n\"Unit:\",\"Index:_1997_Jan_100\"\n\"Multiplier:\",\"1\"\n\"Currency:\",\"NA\"\n\"Unique Identifier: \",\"H10/H10/JRXWTFB_N.B\"\n\"Time Period\",\"JRXWTFB_N.B\"\n";

    private readonly IFedDataDownloadClient _client = Substitute.For<IFedDataDownloadClient>();
    private readonly FedDollarIndexValueProvider _provider;

    public FedDollarIndexValueProviderTests()
    {
        _provider = new FedDollarIndexValueProvider(_client, NullLogger<FedDollarIndexValueProvider>.Instance);
    }

    [Fact(DisplayName = "GetYearAsync pula as seis linhas de cabeçalho e lê data yyyy-MM-dd e valor, em ordem de data")]
    public async Task GetYear_SkipsTheHeader_AndReadsTheRows()
    {
        // Arrange
        ClientReturns(Header + "2026-10-02,121.3848\n2026-10-01,121.7882\n");

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.DollarIndex, 2026);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(new DateOnly(2026, 10, 1), items[0].ReferenceDate);
        Assert.Equal(121.7882m, items[0].Value);
        Assert.Equal(121.3848m, items[1].Value);
        Assert.All(items, item => Assert.Equal(EconomicIndicator.DollarIndex, item.Indicator));
    }

    [Fact(DisplayName = "GetYearAsync pula os dias sem dado (ND) e valores que não são número")]
    public async Task GetYear_SkipsNoDataAndUnusableValues()
    {
        // Arrange
        ClientReturns(Header + "2026-01-01,ND\n2026-01-02,abc\n2026-01-05,100.5\n");

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.DollarIndex, 2026);

        // Assert
        Assert.Equal(100.5m, Assert.Single(items).Value);
    }

    [Fact(DisplayName = "GetYearAsync pede a série do índice amplo e o intervalo do ano em MM/dd/yyyy")]
    public async Task GetYear_RequestsTheBroadIndexSeriesAndTheYear()
    {
        // Arrange
        ClientReturns(Header);

        // Act
        await _provider.GetYearAsync(EconomicIndicator.DollarIndex, 2020);

        // Assert
        await _client.Received(1).GetSeriesAsync("122e3bcb627e8e53f1bf72a1a09cfb81", "01/01/2020", "12/31/2020", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetYearAsync recusa um indicador que não é do Federal Reserve")]
    public async Task GetYear_RejectsAnIndicatorFromAnotherSource()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _provider.GetYearAsync(EconomicIndicator.Vix, 2026));
    }

    private void ClientReturns(string csv) =>
        _client.GetSeriesAsync(default!, default!, default!, default).ReturnsForAnyArgs(csv);
}
