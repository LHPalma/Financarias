using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.DTOs.Results;

public sealed record FxRateSnapshot(
    Currency Base,
    IReadOnlyDictionary<Currency, decimal> Rates,
    DateTimeOffset AsOf
);