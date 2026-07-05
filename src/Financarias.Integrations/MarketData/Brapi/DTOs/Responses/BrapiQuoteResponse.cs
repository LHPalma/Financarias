using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.Brapi.DTOs.Responses;

public sealed record BrapiQuoteResponse(
    [property: JsonPropertyName("results")]
    IReadOnlyList<BrapiQuoteData>? Results
);