using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.MarketData;

public sealed class InvalidTickerException(string? ticker)
    : BaseDomainException("stock.ticker.invalid",
        $"Ticker not valid: '{ticker}'. Expected 4 letters followed by 1-2 digits (e.g. 'PETR4').");