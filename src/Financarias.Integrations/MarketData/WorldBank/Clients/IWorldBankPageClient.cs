using Refit;

namespace Financarias.Integrations.MarketData.WorldBank.Clients;

public interface IWorldBankPageClient
{
    [Get("/en/research/commodity-markets")]
    Task<string> GetCommodityMarketsPageAsync(CancellationToken cancellationToken = default);
}
