using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.B3.CorporateEvents.DTOs.Responses;

public sealed class B3CashDividendsResponse
{
    [JsonPropertyName("page")]
    public B3Page? Page { get; init; }

    [JsonPropertyName("results")]
    public List<B3CashDividend>? Results { get; init; }
}

public sealed class B3Page
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; init; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }
}

public sealed class B3CashDividend
{
    [JsonPropertyName("typeStock")]
    public string? TypeStock { get; init; }

    [JsonPropertyName("dateApproval")]
    public string? DateApproval { get; init; }

    [JsonPropertyName("valueCash")]
    public string? ValueCash { get; init; }

    [JsonPropertyName("ratio")]
    public string? Ratio { get; init; }

    [JsonPropertyName("corporateAction")]
    public string? CorporateAction { get; init; }

    [JsonPropertyName("lastDatePriorEx")]
    public string? LastDatePriorEx { get; init; }
}
