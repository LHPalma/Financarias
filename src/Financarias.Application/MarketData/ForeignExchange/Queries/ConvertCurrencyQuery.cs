using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.Queries;

public sealed record ConvertCurrencyQuery(
    decimal Amount,
    Currency From,
    Currency To,
    int Decimals
) : IQuery<ConversionResult?>;