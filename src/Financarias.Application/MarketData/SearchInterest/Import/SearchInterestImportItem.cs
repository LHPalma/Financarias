namespace Financarias.Application.MarketData.SearchInterest.Import;

public sealed record SearchInterestImportItem(string Term, DateOnly Day, int Value);
