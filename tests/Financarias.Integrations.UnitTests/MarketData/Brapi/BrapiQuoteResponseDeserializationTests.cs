using System.Text.Json;
using Financarias.Integrations.MarketData.Brapi.DTOs.Responses;

namespace Financarias.Integrations.UnitTests.MarketData.Brapi;

public class BrapiQuoteResponseDeserializationTests
{
    // Mirrors what Refit uses under the hood (case-insensitive web defaults).
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact(DisplayName = "Desserializa a resposta da Brapi mapeando os campos da cotação")]
    public void Deserialize_ValidPayload_MapsQuoteFields()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "symbol": "PETR4",
              "shortName": "PETROBRAS PN",
              "currency": "BRL",
              "regularMarketPrice": 38.25,
              "regularMarketChange": 0.29,
              "regularMarketChangePercent": 0.76,
              "regularMarketOpen": 38.07,
              "regularMarketDayHigh": 38.25,
              "regularMarketDayLow": 37.86,
              "regularMarketPreviousClose": 38.24,
              "regularMarketVolume": 10359300,
              "regularMarketTime": "2026-07-03T21:31:30.000Z"
            }
          ]
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<BrapiQuoteResponse>(json, Options);

        // Assert
        Assert.NotNull(response);
        var data = Assert.Single(response!.Results!);
        Assert.Equal("PETR4", data.Symbol);
        Assert.Equal("BRL", data.Currency);
        Assert.Equal(38.25m, data.RegularMarketPrice);
        Assert.Equal(10359300L, data.RegularMarketVolume);
        Assert.Equal(new DateTimeOffset(2026, 7, 3, 21, 31, 30, TimeSpan.Zero), data.RegularMarketTime);
    }
}
