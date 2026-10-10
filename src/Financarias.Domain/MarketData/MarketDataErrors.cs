using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.MarketData;

public static class MarketDataErrors
{
    public const string TickerInvalid = "stock.ticker.invalid";
    public const string PeriodInvalid = "stock.quoteperiod.invalid";

    public static DomainValidationException Ticker(string? ticker) =>
        new(TickerInvalid, $"Ticker not valid: '{ticker}'. Expected a letter, 3 letters or digits, then 1-2 digits (e.g. 'PETR4', 'B3SA3', 'AAPL34').");

    public static DomainValidationException Period(string value) =>
        new(PeriodInvalid, $"Quote period not valid: '{value}'. Expected a date or a year from {QuotePeriod.FirstYear}, or a month from 1 to 12.");
}
