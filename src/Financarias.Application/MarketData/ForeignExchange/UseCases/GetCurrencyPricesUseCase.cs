using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Queries;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.UseCases;

public class GetCurrencyPricesUseCase(
    IQueryHandler<GetCurrencyPricesQuery, IReadOnlyList<CurrencyPriceResult>> handler
) : IGetCurrencyPricesUseCase
{
    public Task<IReadOnlyList<CurrencyPriceResult>> ExecuteAsync(
        IReadOnlyList<Currency> currencies,
        Currency quote = Currency.Brl,
        int decimals = 2,
        CancellationToken cancellationToken = default)
        => handler.HandleAsync(new GetCurrencyPricesQuery(currencies ?? [], quote, decimals), cancellationToken);
}
