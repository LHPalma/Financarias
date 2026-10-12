using Refit;

namespace Financarias.Integrations.MarketData.B3.IndexComposition.Clients;

public interface IB3DataFileClient
{
    [Get("/data/files/{**path}")]
    Task<Stream> GetFileAsync(string path, CancellationToken cancellationToken = default);
}
