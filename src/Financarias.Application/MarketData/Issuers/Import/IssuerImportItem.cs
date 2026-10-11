namespace Financarias.Application.MarketData.Issuers.Import;

public sealed record IssuerImportItem(
    string IssuerCode,
    string CvmCode,
    string CompanyName,
    string TradingName,
    string? Cnpj,
    string? Sector,
    string? Subsector,
    string? Segment,
    string? ListingMarket,
    string? Activity,
    string? Website);
