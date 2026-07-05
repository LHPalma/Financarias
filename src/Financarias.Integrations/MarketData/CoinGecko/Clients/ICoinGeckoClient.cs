using Financarias.Integrations.MarketData.CoinGecko.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.MarketData.CoinGecko.Clients;

public interface ICoinGeckoClient
{
    [Get("/coins/markets")]
    Task<IReadOnlyList<CoinGeckoMarketData>> GetMarketsAsync(
        [AliasAs("vs_currency")] string vsCurrency,
        [AliasAs("ids")] string ids,
        CancellationToken cancellationToken = default
    );
}