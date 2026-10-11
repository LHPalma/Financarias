using Financarias.Domain.MarketData.Indexes;

namespace Financarias.Application.MarketData.Indexes.Import;

public sealed record IndexQuoteImportItem(
    MarketIndex MarketIndex,
    DateOnly TradingDate,
    decimal CloseValue,
    IndexQuoteSource Source);
