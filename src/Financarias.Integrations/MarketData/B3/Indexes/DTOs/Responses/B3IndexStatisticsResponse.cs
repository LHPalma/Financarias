using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.B3.Indexes.DTOs.Responses;

public sealed class B3IndexStatisticsResponse
{
    [JsonPropertyName("results")]
    public List<B3IndexStatisticsRow>? Results { get; init; }
}

public sealed class B3IndexStatisticsRow
{
    [JsonPropertyName("day")]
    public int Day { get; init; }

    [JsonPropertyName("rateValue1")]
    public string? RateValue1 { get; init; }

    [JsonPropertyName("rateValue2")]
    public string? RateValue2 { get; init; }

    [JsonPropertyName("rateValue3")]
    public string? RateValue3 { get; init; }

    [JsonPropertyName("rateValue4")]
    public string? RateValue4 { get; init; }

    [JsonPropertyName("rateValue5")]
    public string? RateValue5 { get; init; }

    [JsonPropertyName("rateValue6")]
    public string? RateValue6 { get; init; }

    [JsonPropertyName("rateValue7")]
    public string? RateValue7 { get; init; }

    [JsonPropertyName("rateValue8")]
    public string? RateValue8 { get; init; }

    [JsonPropertyName("rateValue9")]
    public string? RateValue9 { get; init; }

    [JsonPropertyName("rateValue10")]
    public string? RateValue10 { get; init; }

    [JsonPropertyName("rateValue11")]
    public string? RateValue11 { get; init; }

    [JsonPropertyName("rateValue12")]
    public string? RateValue12 { get; init; }
}
