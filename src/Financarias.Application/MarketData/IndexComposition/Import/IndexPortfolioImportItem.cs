using Financarias.Domain.MarketData.IndexComposition;

namespace Financarias.Application.MarketData.IndexComposition.Import;

public sealed record IndexPortfolioImportItem(
    string IndexCode,
    int Year,
    int Quadrimester,
    IndexPortfolioKind Kind,
    DateOnly? BaseDate,
    decimal? Divisor,
    IReadOnlyList<IndexCompositionRowImportItem> Rows);

public sealed record IndexCompositionRowImportItem(
    string Ticker,
    string AssetName,
    string ShareType,
    long TheoreticalQuantity,
    decimal WeightPercent);
