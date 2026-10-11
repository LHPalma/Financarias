using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.B3.CorporateEvents.DTOs.Responses;

public sealed class B3SupplementCompany
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("stockDividends")]
    public List<B3StockDividend>? StockDividends { get; init; }
}

public sealed class B3StockDividend
{
    [JsonPropertyName("isinCode")]
    public string? IsinCode { get; init; }

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("factor")]
    public string? Factor { get; init; }

    [JsonPropertyName("approvedOn")]
    public string? ApprovedOn { get; init; }

    [JsonPropertyName("lastDatePrior")]
    public string? LastDatePrior { get; init; }
}
