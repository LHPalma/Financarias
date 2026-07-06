using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Gateways;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.ForeignExchange;

namespace Financarias.Application.MarketData.ForeignExchange.Queries;

public class GetCurrencyPricesQueryHandler(
    IFxRateGateway gateway
) : IQueryHandler<GetCurrencyPricesQuery, IReadOnlyList<CurrencyPriceResult>>
{
    private const Currency Pivot = Currency.Brl;

    public async Task<IReadOnlyList<CurrencyPriceResult>> HandleAsync(
        GetCurrencyPricesQuery query,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await gateway.GetLatestRatesAsync(Pivot, cancellationToken);

        if (!snapshot.Rates.TryGetValue(query.Quote, out var quoteRate))
        {
            return [];
        }

        var prices = new List<CurrencyPriceResult>();
        foreach (var currency in query.Currencies)
        {
            if (!snapshot.Rates.TryGetValue(currency, out var rate))
            {
                continue;
            }

            var price = CurrencyConverter.Convert(1m, rate, quoteRate);
            var rounded = Math.Round(price, query.Decimals, MidpointRounding.AwayFromZero);

            prices.Add(new CurrencyPriceResult(currency, query.Quote, rounded, snapshot.AsOf));
        }

        return prices;
    }
}
