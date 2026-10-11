using System.Text;
using System.Text.Json;
using Financarias.Domain.MarketData.Indexes;
using Financarias.Integrations.MarketData.B3.Indexes.Clients;
using Financarias.Integrations.MarketData.B3.Indexes.DTOs.Responses;
using Financarias.Integrations.MarketData.B3.Indexes.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData.B3.Indexes.Providers;

public class B3IndexQuoteProviderTests
{
    // Formato real do endpoint de estatísticas da B3: min/max por mês, e uma linha por dia com uma coluna por mês.
    private const string October2018 =
        """
        {
          "min": { "day": 0, "rateValue10": "78.623,66" },
          "max": { "day": 0, "rateValue10": "87.423,55" },
          "results": [
            { "day": 5, "rateValue9": null, "rateValue10": "82.321,52" },
            { "day": 8, "rateValue9": null, "rateValue10": "86.083,92" },
            { "day": 29, "rateValue10": "83.796,71" }
          ]
        }
        """;

    private readonly IB3IndexClient _client = Substitute.For<IB3IndexClient>();
    private readonly B3IndexQuoteProvider _provider;

    public B3IndexQuoteProviderTests()
    {
        _provider = new B3IndexQuoteProvider(_client, NullLogger<B3IndexQuoteProvider>.Instance);
    }

    [Fact(DisplayName = "GetYearAsync lê a matriz dia × mês, com valor em pt-BR e centavos, ignorando min e max")]
    public async Task GetYear_ParsesTheDayByMonthMatrix()
    {
        // Arrange
        ClientReturns(October2018);

        // Act
        var items = await _provider.GetYearAsync(MarketIndex.Ibovespa, 2018);

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal(new DateOnly(2018, 10, 5), items[0].TradingDate);
        Assert.Equal(82_321.52m, items[0].CloseValue);
        Assert.Equal(new DateOnly(2018, 10, 8), items[1].TradingDate);
        Assert.Equal(86_083.92m, items[1].CloseValue);
        Assert.Equal(new DateOnly(2018, 10, 29), items[2].TradingDate);
        Assert.Equal(83_796.71m, items[2].CloseValue);
        Assert.All(items, item =>
        {
            Assert.Equal(MarketIndex.Ibovespa, item.MarketIndex);
            Assert.Equal(IndexQuoteSource.B3, item.Source);
        });
    }

    [Fact(DisplayName = "GetYearAsync devolve os pregões em ordem de data mesmo com meses misturados")]
    public async Task GetYear_ReturnsItemsOrderedByDate()
    {
        // Arrange
        ClientReturns(
            """
            { "results": [
              { "day": 2, "rateValue1": "77.891,04", "rateValue2": "84.041,34" },
              { "day": 1, "rateValue2": "85.495,24" }
            ] }
            """);

        // Act
        var items = await _provider.GetYearAsync(MarketIndex.Ibovespa, 2018);

        // Assert
        Assert.Equal(
            [new DateOnly(2018, 1, 2), new DateOnly(2018, 2, 1), new DateOnly(2018, 2, 2)],
            items.Select(item => item.TradingDate));
    }

    [Fact(DisplayName = "GetYearAsync descarta dia impossível, valor não positivo e texto que não é número")]
    public async Task GetYear_DiscardsUnusableValues()
    {
        // Arrange
        ClientReturns(
            """
            { "results": [
              { "day": 31, "rateValue11": "1.000,00" },
              { "day": 9, "rateValue10": "0,00" },
              { "day": 10, "rateValue10": "abc" },
              { "day": 11, "rateValue10": "83.000,10" }
            ] }
            """);

        // Act
        var items = await _provider.GetYearAsync(MarketIndex.Ibovespa, 2018);

        // Assert
        var item = Assert.Single(items);
        Assert.Equal(new DateOnly(2018, 10, 11), item.TradingDate);
    }

    [Fact(DisplayName = "GetYearAsync devolve lista vazia para um ano sem dados (results nulo)")]
    public async Task GetYear_ReturnsEmpty_WhenResultsIsNull()
    {
        // Arrange
        ClientReturns("""{ "min": null, "max": null, "results": null }""");

        // Act
        var items = await _provider.GetYearAsync(MarketIndex.Ibovespa, 2040);

        // Assert
        Assert.Empty(items);
    }

    [Fact(DisplayName = "GetYearAsync pede o índice IBOV e o ano em JSON codificado em base64")]
    public async Task GetYear_SendsTheBase64Payload()
    {
        // Arrange
        string? payload = null;
        _client.GetYearAsync(Arg.Do<string>(sent => payload = sent), Arg.Any<CancellationToken>())
            .Returns(new B3IndexStatisticsResponse());

        // Act
        await _provider.GetYearAsync(MarketIndex.Ibovespa, 2018);

        // Assert
        Assert.Equal(
            """{"language":"pt-br","index":"IBOV","year":"2018"}""",
            Encoding.UTF8.GetString(Convert.FromBase64String(payload!)));
    }

    [Fact(DisplayName = "GetYearAsync propaga a falha de transporte")]
    public async Task GetYear_PropagatesTransportFailure()
    {
        // Arrange
        _client.GetYearAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<B3IndexStatisticsResponse>(_ => throw new HttpRequestException("boom"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _provider.GetYearAsync(MarketIndex.Ibovespa, 2018));
    }

    private void ClientReturns(string json) =>
        _client.GetYearAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Deserialize<B3IndexStatisticsResponse>(json)!);
}
