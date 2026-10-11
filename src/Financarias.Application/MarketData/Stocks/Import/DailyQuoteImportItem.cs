using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;

namespace Financarias.Application.MarketData.Stocks.Import;

public sealed record DailyQuoteImportItem(
    Ticker Ticker,
    DateOnly TradingDate,
    QuoteMarketType MarketType,
    string BdiCode,
    string CompanyName,
    string Specification,
    string Isin,
    decimal OpenPrice,
    decimal HighPrice,
    decimal LowPrice,
    decimal AveragePrice,
    decimal ClosePrice,
    int Trades,
    long Quantity,
    decimal Volume,
    int QuoteFactor,
    string? LegacyTicker = null);
