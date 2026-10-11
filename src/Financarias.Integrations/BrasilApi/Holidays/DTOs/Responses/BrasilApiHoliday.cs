using System.Text.Json.Serialization;

namespace Financarias.Integrations.BrasilApi.Holidays.DTOs.Responses;

public sealed class BrasilApiHoliday
{
    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}
