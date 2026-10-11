using System.Text.Json.Serialization;

namespace Financarias.Integrations.MarketData.B3.Issuers.DTOs.Responses;

public sealed class B3InitialCompaniesResponse
{
    [JsonPropertyName("page")]
    public B3CompaniesPage? Page { get; init; }

    [JsonPropertyName("results")]
    public List<B3ListedCompany>? Results { get; init; }
}

public sealed class B3CompaniesPage
{
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }
}

public sealed class B3ListedCompany
{
    [JsonPropertyName("codeCVM")]
    public string? CodeCvm { get; init; }

    [JsonPropertyName("issuingCompany")]
    public string? IssuingCompany { get; init; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; init; }

    [JsonPropertyName("tradingName")]
    public string? TradingName { get; init; }

    [JsonPropertyName("cnpj")]
    public string? Cnpj { get; init; }
}
