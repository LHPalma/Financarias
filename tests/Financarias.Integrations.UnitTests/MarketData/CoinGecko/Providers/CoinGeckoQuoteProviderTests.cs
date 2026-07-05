using Financarias.Domain.MarketData.Cryptos;
using Financarias.Integrations.MarketData.CoinGecko.Clients;
using Financarias.Integrations.MarketData.CoinGecko.DTOs.Responses;
using Financarias.Integrations.MarketData.CoinGecko.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Financarias.Integrations.UnitTests.MarketData.CoinGecko.Providers;

public class CoinGeckoQuoteProviderTests
{
    private readonly ICoinGeckoClient _client;
    private readonly CoinGeckoQuoteProvider _provider;

    public CoinGeckoQuoteProviderTests()
    {
        _client = Substitute.For<ICoinGeckoClient>();
        _provider = new CoinGeckoQuoteProvider(_client, Substitute.For<ILogger<CoinGeckoQuoteProvider>>());
    }

    private static CoinGeckoMarketData Data(string id, string symbol = "x", string name = "X", decimal? price = 100m) =>
        new(
            Id: id,
            Symbol: symbol,
            Name: name,
            CurrentPrice: price,
            MarketCap: 1000m,
            TotalVolume: 500m,
            High24h: 110m,
            Low24h: 90m,
            PriceChange24h: 5m,
            PriceChangePercentage24h: 1.5m,
            LastUpdated: new DateTimeOffset(2026, 7, 5, 4, 0, 0, TimeSpan.Zero));

    [Fact(DisplayName = "Mapeia a resposta do CoinGecko para a cotação interna")]
    public async Task FindQuotes_WithValidResponse_MapsFields()
    {
        // Arrange
        _client.GetMarketsAsync("brl", "bitcoin", Arg.Any<CancellationToken>())
            .Returns(new[] { Data("bitcoin", "btc", "Bitcoin", 325234m) });

        // Act
        var result = await _provider.FindQuotesAsync([CryptoAsset.Bitcoin], QuoteCurrency.Brl);

        // Assert
        var quote = Assert.Single(result);
        Assert.Equal(CryptoAsset.Bitcoin, quote.Asset);
        Assert.Equal("btc", quote.Symbol);
        Assert.Equal(QuoteCurrency.Brl, quote.Currency);
        Assert.Equal(325234m, quote.Price);
        Assert.Equal(1.5m, quote.PriceChangePercent24h);
    }

    [Fact(DisplayName = "Traduz Xrp->ripple no request e ripple->Xrp na resposta (bidirecional)")]
    public async Task FindQuotes_WithXrp_MapsBidirectionally()
    {
        // Arrange
        _client.GetMarketsAsync("brl", "ripple", Arg.Any<CancellationToken>())
            .Returns(new[] { Data("ripple", "xrp", "XRP", 5.9m) });

        // Act
        var result = await _provider.FindQuotesAsync([CryptoAsset.Xrp], QuoteCurrency.Brl);

        // Assert
        Assert.Equal(CryptoAsset.Xrp, Assert.Single(result).Asset);
    }

    [Fact(DisplayName = "Mapeia a moeda do input para o vs_currency em minúsculas")]
    public async Task FindQuotes_WithUsd_SendsLowercaseCurrency()
    {
        // Arrange
        _client.GetMarketsAsync("usd", "bitcoin", Arg.Any<CancellationToken>())
            .Returns(new[] { Data("bitcoin") });

        // Act
        var result = await _provider.FindQuotesAsync([CryptoAsset.Bitcoin], QuoteCurrency.Usd);

        // Assert
        Assert.Equal(QuoteCurrency.Usd, Assert.Single(result).Currency);
    }

    [Fact(DisplayName = "Ignora item com id que não mapeia para nenhum CryptoAsset")]
    public async Task FindQuotes_WithUnknownId_SkipsItem()
    {
        // Arrange
        _client.GetMarketsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new[] { Data("dogecoin", price: 1m), Data("some-random-coin", price: 2m) });

        // Act
        var result = await _provider.FindQuotesAsync([CryptoAsset.Dogecoin], QuoteCurrency.Brl);

        // Assert
        Assert.Equal(CryptoAsset.Dogecoin, Assert.Single(result).Asset);
    }

    [Fact(DisplayName = "Ignora item sem preço (miss)")]
    public async Task FindQuotes_WithoutPrice_SkipsItem()
    {
        // Arrange
        _client.GetMarketsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new[] { Data("bitcoin", price: null) });

        // Act
        var result = await _provider.FindQuotesAsync([CryptoAsset.Bitcoin], QuoteCurrency.Brl);

        // Assert
        Assert.Empty(result);
    }

    [Fact(DisplayName = "Lista de ativos vazia retorna vazio sem chamar o cliente")]
    public async Task FindQuotes_WithNoAssets_ReturnsEmptyWithoutCallingClient()
    {
        // Act
        var result = await _provider.FindQuotesAsync([], QuoteCurrency.Brl);

        // Assert
        Assert.Empty(result);
        await _client.DidNotReceive().GetMarketsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Propaga a exceção quando o transporte falha (não é miss)")]
    public async Task FindQuotes_WhenTransportFails_PropagatesException()
    {
        // Arrange
        _client.GetMarketsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _provider.FindQuotesAsync([CryptoAsset.Bitcoin], QuoteCurrency.Brl));
    }
}
