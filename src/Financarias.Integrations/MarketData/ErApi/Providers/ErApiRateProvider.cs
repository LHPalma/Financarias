using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Gateways;
using Financarias.Domain.MarketData;
using Financarias.Integrations.MarketData.ErApi.Clients;

namespace Financarias.Integrations.MarketData.ErApi.Providers;

public sealed class ErApiRateProvider(
    IErApiClient client
) : IFxRateGateway
{
    public async Task<FxRateSnapshot> GetLatestRatesAsync(
        Currency baseCurrency,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetLatestAsync(baseCurrency.ToString().ToUpperInvariant(), cancellationToken);

        var responseRates = response.Rates ?? new Dictionary<string, decimal>();

        var rates = new Dictionary<Currency, decimal>();
        foreach (var currency in Enum.GetValues<Currency>())
        {
            if (responseRates.TryGetValue(currency.ToString().ToUpperInvariant(), out var rate))
            {
                rates[currency] = rate;
            }
        }

        var asOf = DateTimeOffset.FromUnixTimeSeconds(response.TimeLastUpdateUnix);

        return new FxRateSnapshot(baseCurrency, rates, asOf);
    }
}
