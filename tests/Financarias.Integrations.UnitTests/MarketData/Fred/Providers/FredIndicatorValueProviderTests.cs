using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Fred;
using Financarias.Integrations.MarketData.Fred.Clients;
using Financarias.Integrations.MarketData.Fred.DTOs.Responses;
using Financarias.Integrations.MarketData.Fred.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData.Fred.Providers;

public class FredIndicatorValueProviderTests
{
    private readonly IFredClient _client = Substitute.For<IFredClient>();
    private readonly FredIndicatorValueProvider _provider;

    public FredIndicatorValueProviderTests()
    {
        _provider = CreateProvider("test-key");
    }

    [Fact(DisplayName = "GetYearAsync lê as observações do FRED e devolve em ordem de data")]
    public async Task GetYear_ParsesTheObservations_InDateOrder()
    {
        // Arrange
        ClientReturns(Observation("2026-01-06", "62.10"), Observation("2026-01-05", "63.00"));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.BrentOil, 2026);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(new DateOnly(2026, 1, 5), items[0].ReferenceDate);
        Assert.Equal(63.00m, items[0].Value);
        Assert.Equal(62.10m, items[1].Value);
        Assert.All(items, item => Assert.Equal(EconomicIndicator.BrentOil, item.Indicator));
    }

    [Fact(DisplayName = "GetYearAsync pula os dias sem observação, que o FRED marca com ponto")]
    public async Task GetYear_SkipsMissingObservations()
    {
        // Arrange
        ClientReturns(Observation("2026-01-01", "."), Observation("2026-01-02", "57.32"));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.WtiOil, 2026);

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 2), Assert.Single(items).ReferenceDate);
    }

    [Fact(DisplayName = "GetYearAsync arredonda a seis casas, que é a precisão guardada")]
    public async Task GetYear_RoundsToStoredPrecision()
    {
        // Arrange
        ClientReturns(Observation("2026-06-01", "103.79090909090910"));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.IronOre, 2026);

        // Assert
        Assert.Equal(103.790909m, Assert.Single(items).Value);
    }

    [Fact(DisplayName = "GetYearAsync descarta observação com data ou valor inválido")]
    public async Task GetYear_DiscardsUnusableObservations()
    {
        // Arrange
        ClientReturns(
            Observation("not-a-date", "15.1"),
            Observation("2026-01-02", "abc"),
            Observation(null, "15.1"),
            Observation("2026-01-06", null),
            Observation("2026-01-05", "16.2"));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.Vix, 2026);

        // Assert
        Assert.Equal(16.2m, Assert.Single(items).Value);
    }

    [Fact(DisplayName = "GetYearAsync com resposta sem observações devolve lista vazia")]
    public async Task GetYear_WithoutObservations_ReturnsEmpty()
    {
        // Arrange
        _client.GetObservationsAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(new FredObservationsResponse());

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.DollarIndex, 1999);

        // Assert
        Assert.Empty(items);
    }

    [Fact(DisplayName = "GetYearAsync pede ao FRED a série do indicador, o intervalo do ano e a chave")]
    public async Task GetYear_RequestsTheSeriesTheYearAndTheKey()
    {
        // Arrange
        ClientReturns();

        // Act
        await _provider.GetYearAsync(EconomicIndicator.DollarIndex, 2020);

        // Assert
        await _client.Received(1).GetObservationsAsync("DTWEXBGS", "2020-01-01", "2020-12-31", "test-key", Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "GetYearAsync sem chave configurada falha com mensagem que diz como configurar, sem chamar a rede")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetYear_WithoutApiKey_FailsBeforeCallingTheNetwork(string? apiKey)
    {
        // Arrange
        var provider = CreateProvider(apiKey);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetYearAsync(EconomicIndicator.BrentOil, 2026));

        // Assert
        Assert.Contains("Integrations:Fred:ApiKey", exception.Message, StringComparison.Ordinal);
        await _client.DidNotReceiveWithAnyArgs().GetObservationsAsync(default!, default!, default!, default!, default);
    }

    [Fact(DisplayName = "GetYearAsync recusa um indicador que não é do FRED")]
    public async Task GetYear_RejectsAnIndicatorFromAnotherSource()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _provider.GetYearAsync(EconomicIndicator.Ipca, 2026));
    }

    private static FredObservation Observation(string? date, string? value) => new() { Date = date, Value = value };

    private FredIndicatorValueProvider CreateProvider(string? apiKey) =>
        new(_client, Options.Create(new FredOptions { ApiKey = apiKey }), NullLogger<FredIndicatorValueProvider>.Instance);

    private void ClientReturns(params FredObservation[] observations) =>
        _client.GetObservationsAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(new FredObservationsResponse { Observations = [.. observations] });
}
