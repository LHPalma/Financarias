using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Gateways;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.ForeignExchange;

namespace Financarias.Application.MarketData.ForeignExchange.Queries;

public class ConvertCurrencyQueryHandler(
    IFxRateGateway gateway
) : IQueryHandler<ConvertCurrencyQuery, ConversionResult?>
{
    public async Task<ConversionResult?> HandleAsync(
        ConvertCurrencyQuery query,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await gateway.GetLatestRatesAsync(Currency.Brl, cancellationToken);

        if (!snapshot.Rates.TryGetValue(query.From, out var rateFrom) ||
            !snapshot.Rates.TryGetValue(query.To, out var rateTo))
        {
            return null;
        }

        var converted = CurrencyConverter.Convert(query.Amount, rateFrom, rateTo);
        var rounded = Math.Round(converted, query.Decimals, MidpointRounding.AwayFromZero);
        var rate = CurrencyConverter.CrossRate(rateFrom, rateTo);

        return new ConversionResult(query.From, query.To, query.Amount, rounded, rate, snapshot.AsOf);
    }
}