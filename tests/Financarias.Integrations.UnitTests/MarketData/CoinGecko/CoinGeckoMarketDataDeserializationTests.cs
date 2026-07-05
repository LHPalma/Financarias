using System.Text.Json;
using Financarias.Integrations.MarketData.CoinGecko.DTOs.Responses;

namespace Financarias.Integrations.UnitTests.MarketData.CoinGecko;

public class CoinGeckoMarketDataDeserializationTests
{
    // Mirrors what Refit uses under the hood (case-insensitive web defaults).
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact(DisplayName = "Desserializa o item do CoinGecko mapeando os campos snake_case")]
    public void Deserialize_ValidPayload_MapsFields()
    {
        // Arrange
        const string json = """
        {
          "id": "bitcoin",
          "symbol": "btc",
          "name": "Bitcoin",
          "current_price": 325234,
          "market_cap": 6522460408172,
          "total_volume": 91869412950,
          "high_24h": 328637,
          "low_24h": 323101,
          "price_change_24h": 807.09,
          "price_change_percentage_24h": 0.24878,
          "last_updated": "2026-07-05T04:00:31.447Z"
        }
        """;

        // Act
        var data = JsonSerializer.Deserialize<CoinGeckoMarketData>(json, Options);

        // Assert
        Assert.NotNull(data);
        Assert.Equal("bitcoin", data!.Id);
        Assert.Equal("btc", data.Symbol);
        Assert.Equal(325234m, data.CurrentPrice);
        Assert.Equal(91869412950m, data.TotalVolume);
        Assert.Equal(0.24878m, data.PriceChangePercentage24h);
        Assert.Equal(new DateTimeOffset(2026, 7, 5, 4, 0, 31, 447, TimeSpan.Zero), data.LastUpdated);
    }
}
