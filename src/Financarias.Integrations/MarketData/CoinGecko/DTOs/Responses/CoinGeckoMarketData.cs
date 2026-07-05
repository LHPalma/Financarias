using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.CoinGecko.DTOs.Responses;

// @formatter:off
public sealed record CoinGeckoMarketData(
    [property: JsonPropertyName("id")]
    string? Id,

    [property: JsonPropertyName("symbol")]
    string? Symbol,

    [property: JsonPropertyName("name")]
    string? Name,

    [property: JsonPropertyName("current_price")]
    decimal? CurrentPrice,

    [property: JsonPropertyName("market_cap")]
    decimal? MarketCap,

    [property: JsonPropertyName("total_volume")]
    decimal? TotalVolume,

    [property: JsonPropertyName("high_24h")]
    decimal? High24h,

    [property: JsonPropertyName("low_24h")]
    decimal? Low24h,

    [property: JsonPropertyName("price_change_24h")]
    decimal? PriceChange24h,

    [property: JsonPropertyName("price_change_percentage_24h")]
    decimal? PriceChangePercentage24h,

    [property: JsonPropertyName("last_updated")]
    DateTimeOffset? LastUpdated
);
// @formatter:on