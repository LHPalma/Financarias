using Refit;

namespace Financarias.Integrations.MarketData.Cboe.Clients;

public interface ICboeClient
{
    [Get("/api/global/us_indices/daily_prices/VIX_History.csv")]
    Task<string> GetVixHistoryAsync(CancellationToken cancellationToken = default);
}
