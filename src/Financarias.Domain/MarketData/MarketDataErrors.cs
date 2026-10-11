using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.MarketData;

public static class MarketDataErrors
{
    public const string TickerInvalid = "stock.ticker.invalid";
    public const string PeriodInvalid = "stock.quoteperiod.invalid";
    public const string QuoteInvalid = "stock.quote.invalid";
    public const string IndexQuoteInvalid = "index.quote.invalid";
    public const string IndexYearInvalid = "index.year.invalid";
    public const string IndicatorYearInvalid = "indicator.year.invalid";
    public const string CorporateEventInvalid = "corporate.event.invalid";

    public static DomainValidationException Ticker(string? ticker) =>
        new(TickerInvalid, $"Ticker not valid: '{ticker}'. Expected a letter, 3 letters or digits, then 1-2 digits (e.g. 'PETR4', 'B3SA3', 'AAPL34').");

    public static DomainValidationException Period(string value) =>
        new(PeriodInvalid, $"Quote period not valid: '{value}'. Expected a date or a year from {QuotePeriod.FirstYear}, or a month from 1 to 12.");

    public static DomainValidationException Quote(string reason) =>
        new(QuoteInvalid, $"Daily quote not valid: {reason}.");

    public static DomainValidationException IndexQuote(string reason) =>
        new(IndexQuoteInvalid, $"Index quote not valid: {reason}.");

    public static DomainValidationException IndexYear(int year) =>
        new(IndexYearInvalid, $"Index year not valid: {year}. Expected a year from {Indexes.IndexQuote.FirstYear}.");

    public static DomainValidationException IndicatorYear(int year) =>
        new(IndicatorYearInvalid, $"Indicator year not valid: {year}. Expected a year from {Indicators.IndicatorValue.FirstYear}.");

    public static DomainValidationException CorporateEvent(string reason) =>
        new(CorporateEventInvalid, $"Corporate event not valid: {reason}.");
}
