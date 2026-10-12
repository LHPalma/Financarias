namespace Financarias.Application.MarketData.IndexComposition.Import;

public sealed record IndexCompositionFileImportResult(
    int Portfolios,
    int Rows,
    int Created,
    int Updated);
