using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.MarketData;

public static class MarketDataErrors
{
    public const string TickerInvalid = "stock.ticker.invalid";

    public static DomainValidationException Ticker(string? ticker) =>
        new(TickerInvalid, $"Ticker not valid: '{ticker}'. Expected 4 letters followed by 1-2 digits (e.g. 'PETR4').");
}
