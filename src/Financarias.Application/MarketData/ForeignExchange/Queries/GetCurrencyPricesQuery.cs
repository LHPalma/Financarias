using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.Queries;

public sealed record GetCurrencyPricesQuery(
    IReadOnlyList<Currency> Currencies,
    Currency Quote,
    int Decimals
) : IQuery<IReadOnlyList<CurrencyPriceResult>>;
