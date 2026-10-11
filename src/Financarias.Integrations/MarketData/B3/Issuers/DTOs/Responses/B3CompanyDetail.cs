using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.B3.Issuers.DTOs.Responses;

public sealed class B3CompanyDetail
{
    [JsonPropertyName("industryClassification")]
    public string? IndustryClassification { get; init; }

    [JsonPropertyName("activity")]
    public string? Activity { get; init; }

    [JsonPropertyName("website")]
    public string? Website { get; init; }

    [JsonPropertyName("market")]
    public string? Market { get; init; }
}
