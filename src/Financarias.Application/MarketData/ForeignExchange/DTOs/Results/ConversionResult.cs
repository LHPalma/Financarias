using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.DTOs.Results;

public sealed record ConversionResult(
    Currency From,
    Currency To,
    decimal Amount,
    decimal ConvertedAmount,
    decimal Rate,
    DateTimeOffset AsOf
);