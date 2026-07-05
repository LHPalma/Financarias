using System.Net;
using Financarias.Domain.MarketData;
using Financarias.Integrations.MarketData.Brapi.Clients;
using Financarias.Integrations.MarketData.Brapi.DTOs.Responses;
using Financarias.Integrations.MarketData.Brapi.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Refit;

namespace Financarias.Integrations.UnitTests.MarketData.Brapi.Providers;

public class BrapiQuoteProviderTests
{
    private readonly IBrapiClient _client;
    private readonly BrapiQuoteProvider _provider;

    public BrapiQuoteProviderTests()
    {
        _client = Substitute.For<IBrapiClient>();
        _provider = new BrapiQuoteProvider(_client, Substitute.For<ILogger<BrapiQuoteProvider>>());
    }

    private static BrapiQuoteData SampleData(decimal? price = 38.25m, string? currency = "BRL") =>
        new(
            "PETR4", "PETROBRAS PN", currency, price, 0.29m, 0.76m,
            38.07m, 38.25m, 37.86m, 38.24m, 10359300L,
            new DateTimeOffset(2026, 7, 3, 21, 31, 30, TimeSpan.Zero));

    [Fact(DisplayName = "Mapeia a resposta da Brapi para a cotação interna")]
    public async Task FindQuote_WithValidResponse_MapsAllFields()
    {
        // Arrange
        _client.GetQuoteAsync("PETR4", Arg.Any<CancellationToken>())
            .Returns(new BrapiQuoteResponse(new[] { SampleData() }));

        // Act
        var result = await _provider.FindQuoteAsync(Ticker.Create("PETR4"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PETR4", result!.Symbol);
        Assert.Equal("PETROBRAS PN", result.Name);
        Assert.Equal("BRL", result.Currency);
        Assert.Equal(38.25m, result.Price);
        Assert.Equal(0.29m, result.Change);
        Assert.Equal(0.76m, result.ChangePercent);
        Assert.Equal(38.07m, result.Open);
        Assert.Equal(38.25m, result.DayHigh);
        Assert.Equal(37.86m, result.DayLow);
        Assert.Equal(38.24m, result.PreviousClose);
        Assert.Equal(10359300L, result.Volume);
        Assert.Equal(new DateTimeOffset(2026, 7, 3, 21, 31, 30, TimeSpan.Zero), result.AsOf);
    }

    [Fact(DisplayName = "Usa BRL como moeda padrão quando a Brapi não informa currency")]
    public async Task FindQuote_WithoutCurrency_DefaultsToBrl()
    {
        // Arrange
        _client.GetQuoteAsync("PETR4", Arg.Any<CancellationToken>())
            .Returns(new BrapiQuoteResponse(new[] { SampleData(currency: null) }));

        // Act
        var result = await _provider.FindQuoteAsync(Ticker.Create("PETR4"));

        // Assert
        Assert.Equal("BRL", result!.Currency);
    }

    [Fact(DisplayName = "Retorna nulo (miss) quando a Brapi responde 404")]
    public async Task FindQuote_WhenBrapiReturns404_ReturnsNull()
    {
        // Arrange
        var notFound = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://brapi.dev/api/quote/XXXX4"),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new RefitSettings());
        _client.GetQuoteAsync("XXXX4", Arg.Any<CancellationToken>()).ThrowsAsync(notFound);

        // Act
        var result = await _provider.FindQuoteAsync(Ticker.Create("XXXX4"));

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "Retorna nulo (miss) quando a Brapi devolve results vazio")]
    public async Task FindQuote_WhenResultsEmpty_ReturnsNull()
    {
        // Arrange
        _client.GetQuoteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BrapiQuoteResponse(Array.Empty<BrapiQuoteData>()));

        // Act
        var result = await _provider.FindQuoteAsync(Ticker.Create("PETR4"));

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "Retorna nulo (miss) quando a cotação vem sem preço")]
    public async Task FindQuote_WhenPriceMissing_ReturnsNull()
    {
        // Arrange
        _client.GetQuoteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BrapiQuoteResponse(new[] { SampleData(price: null) }));

        // Act
        var result = await _provider.FindQuoteAsync(Ticker.Create("PETR4"));

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "Propaga a exceção quando o transporte falha (não é miss)")]
    public async Task FindQuote_WhenTransportFails_PropagatesException()
    {
        // Arrange
        _client.GetQuoteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _provider.FindQuoteAsync(Ticker.Create("PETR4")));
    }

    [Fact(DisplayName = "Propaga ApiException não-404 (falha da fonte, não miss)")]
    public async Task FindQuote_WhenNon404ApiException_PropagatesException()
    {
        // Arrange
        var serverError = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://brapi.dev/api/quote/PETR4"),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new RefitSettings());
        _client.GetQuoteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(serverError);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => _provider.FindQuoteAsync(Ticker.Create("PETR4")));
    }
}
