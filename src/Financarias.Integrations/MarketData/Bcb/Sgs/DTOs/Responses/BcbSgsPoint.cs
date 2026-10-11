using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.Bcb.Sgs.DTOs.Responses;

public sealed class BcbSgsPoint
{
    [JsonPropertyName("data")]
    public string? Data { get; init; }

    [JsonPropertyName("valor")]
    public string? Valor { get; init; }
}
