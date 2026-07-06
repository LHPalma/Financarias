using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.DTOs.Results;

public sealed record CurrencyPriceResult(
    Currency Currency,
    Currency Quote,
    decimal Price,
    DateTimeOffset AsOf
);
