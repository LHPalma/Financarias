using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.Fred.DTOs.Responses;

public sealed class FredObservationsResponse
{
    [JsonPropertyName("observations")]
    public List<FredObservation>? Observations { get; init; }
}

public sealed class FredObservation
{
    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
