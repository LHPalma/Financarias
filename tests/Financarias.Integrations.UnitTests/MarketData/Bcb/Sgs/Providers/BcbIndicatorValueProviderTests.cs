using System.Net;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Bcb.Sgs.Clients;
using Financarias.Integrations.MarketData.Bcb.Sgs.DTOs.Responses;
using Financarias.Integrations.MarketData.Bcb.Sgs.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Refit;

namespace Financarias.Integrations.UnitTests.MarketData.Bcb.Sgs.Providers;

public class BcbIndicatorValueProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly IBcbSgsClient _client = Substitute.For<IBcbSgsClient>();
    private readonly BcbIndicatorValueProvider _provider;

    public BcbIndicatorValueProviderTests()
    {
        _provider = new BcbIndicatorValueProvider(_client, new FixedTimeProvider(Now), NullLogger<BcbIndicatorValueProvider>.Instance);
    }

    [Fact(DisplayName = "GetYearAsync lê data dd/MM/yyyy e valor com ponto decimal, em ordem de data")]
    public async Task GetYear_ParsesDatesAndValues_InDateOrder()
    {
        // Arrange
        ClientReturns(Point("09/10/2026", "4.9892"), Point("08/10/2026", "5.0119"));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.Dollar, 2026);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(new DateOnly(2026, 10, 8), items[0].ReferenceDate);
        Assert.Equal(5.0119m, items[0].Value);
        Assert.Equal(new DateOnly(2026, 10, 9), items[1].ReferenceDate);
        Assert.Equal(4.9892m, items[1].Value);
        Assert.All(items, item => Assert.Equal(EconomicIndicator.Dollar, item.Indicator));
    }

    [Fact(DisplayName = "GetYearAsync aceita valor negativo (deflação do IPCA)")]
    public async Task GetYear_AcceptsNegativeValue()
    {
        // Arrange
        ClientReturns(Point("01/08/2026", "-0.32"));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.Ipca, 2026);

        // Assert
        Assert.Equal(-0.32m, Assert.Single(items).Value);
    }

    [Fact(DisplayName = "GetYearAsync descarta datas futuras, como a meta Selic que o Banco Central já publica adiante")]
    public async Task GetYear_DiscardsFutureDates()
    {
        // Arrange
        ClientReturns(Point("09/10/2026", "13.75"), Point("11/10/2026", "13.75"), Point("04/11/2026", "13.75"));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.SelicTarget, 2026);

        // Assert
        Assert.Equal(
            [new DateOnly(2026, 10, 9), new DateOnly(2026, 10, 11)],
            items.Select(item => item.ReferenceDate));
    }

    [Fact(DisplayName = "GetYearAsync descarta data inválida, valor que não é número e campos vazios")]
    public async Task GetYear_DiscardsUnusablePoints()
    {
        // Arrange
        ClientReturns(
            Point("31/02/2026", "5.00"),
            Point("08/10/2026", "abc"),
            Point(null, "5.00"),
            Point("09/10/2026", null),
            Point("02/10/2026", "5.1234"));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.Dollar, 2026);

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 2), Assert.Single(items).ReferenceDate);
    }

    [Theory(DisplayName = "GetYearAsync pede a série certa do SGS e a janela do ano inteiro")]
    [InlineData(EconomicIndicator.Dollar, 1)]
    [InlineData(EconomicIndicator.SelicTarget, 432)]
    [InlineData(EconomicIndicator.Cdi, 4389)]
    [InlineData(EconomicIndicator.Ipca, 433)]
    public async Task GetYear_RequestsTheSeriesAndTheYearWindow(EconomicIndicator indicator, int series)
    {
        // Arrange
        ClientReturns();

        // Act
        await _provider.GetYearAsync(indicator, 2018);

        // Assert
        await _client.Received(1).GetSeriesAsync(series, "01/01/2018", "31/12/2018", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetYearAsync devolve lista vazia quando o ano não tem dados (404 do Banco Central)")]
    public async Task GetYear_ReturnsEmpty_On404()
    {
        // Arrange
        var notFound = await ApiExceptionFor(HttpStatusCode.NotFound);
        _client.GetSeriesAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<List<BcbSgsPoint>>(_ => throw notFound);

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.Dollar, 1980);

        // Assert
        Assert.Empty(items);
    }

    [Fact(DisplayName = "GetYearAsync propaga falha de transporte diferente de 404")]
    public async Task GetYear_Propagates_NonNotFoundFailures()
    {
        // Arrange
        var serverError = await ApiExceptionFor(HttpStatusCode.InternalServerError);
        _client.GetSeriesAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<List<BcbSgsPoint>>(_ => throw serverError);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => _provider.GetYearAsync(EconomicIndicator.Dollar, 2026));
    }

    private static BcbSgsPoint Point(string? date, string? value) => new() { Data = date, Valor = value };

    private static Task<ApiException> ApiExceptionFor(HttpStatusCode status) =>
        ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://api.bcb.gov.br/x"),
            HttpMethod.Get,
            new HttpResponseMessage(status),
            new RefitSettings());

    private void ClientReturns(params BcbSgsPoint[] points) =>
        _client.GetSeriesAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(points.ToList());

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
