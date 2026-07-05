namespace Financarias.Application.MarketData.Stocks.DTOs.Results;

public sealed record StockQuoteResult(
    string Symbol,
    string? Name,
    string Currency,
    decimal Price,
    decimal? Change,
    decimal? ChangePercent,
    decimal? Open,
    decimal? DayHigh,
    decimal? DayLow,
    decimal? PreviousClose,
    long? Volume,
    DateTimeOffset AsOf
);