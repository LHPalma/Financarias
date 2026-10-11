using Refit;

namespace Financarias.Integrations.MarketData.Eia.Clients;

public interface IEiaClient
{
    [Get("/dnav/pet/hist_xls/{file}")]
    Task<Stream> GetSpotPricesFileAsync(string file, CancellationToken cancellationToken = default);
}
