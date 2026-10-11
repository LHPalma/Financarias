using Refit;

namespace Financarias.Integrations.MarketData.WorldBank.Clients;

public interface IWorldBankDocsClient
{
    [Get("/en/doc/{release}/related/CMO-Historical-Data-Monthly.xlsx")]
    Task<Stream> GetMonthlyPricesFileAsync(string release, CancellationToken cancellationToken = default);
}
