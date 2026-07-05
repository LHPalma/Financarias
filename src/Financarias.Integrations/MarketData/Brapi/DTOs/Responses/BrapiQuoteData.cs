using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.Brapi.DTOs.Responses;

// @formatter:off
public sealed record BrapiQuoteData(
    [property: JsonPropertyName("symbol")]
    string? Symbol,

    [property: JsonPropertyName("shortName")]
    string? ShortName,

    [property: JsonPropertyName("currency")]
    string? Currency,

    [property: JsonPropertyName("regularMarketPrice")]
    decimal? RegularMarketPrice,

    [property: JsonPropertyName("regularMarketChange")]
    decimal? RegularMarketChange,

    [property: JsonPropertyName("regularMarketChangePercent")]
    decimal? RegularMarketChangePercent,

    [property: JsonPropertyName("regularMarketOpen")]
    decimal? RegularMarketOpen,

    [property: JsonPropertyName("regularMarketDayHigh")]
    decimal? RegularMarketDayHigh,

    [property: JsonPropertyName("regularMarketDayLow")]
    decimal? RegularMarketDayLow,

    [property: JsonPropertyName("regularMarketPreviousClose")]
    decimal? RegularMarketPreviousClose,

    [property: JsonPropertyName("regularMarketVolume")]
    long? RegularMarketVolume,

    [property: JsonPropertyName("regularMarketTime")]
    DateTimeOffset? RegularMarketTime
);
// @formatter:on