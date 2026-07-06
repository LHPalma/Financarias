using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.ErApi.DTOs.Responses;

// @formatter:off
public sealed record ErApiLatestResponse(
    [property: JsonPropertyName("result")]
    string? Result,

    [property: JsonPropertyName("base_code")]
    string? BaseCode,

    [property: JsonPropertyName("time_last_update_unix")]
    long TimeLastUpdateUnix,

    [property: JsonPropertyName("rates")]
    IReadOnlyDictionary<string, decimal>? Rates
);
// @formatter:on
