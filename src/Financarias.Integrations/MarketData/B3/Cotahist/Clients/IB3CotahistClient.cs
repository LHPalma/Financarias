using Refit;

namespace Financarias.Integrations.MarketData.B3.Cotahist.Clients;

public interface IB3CotahistClient
{
    [Get("/InstDados/SerHist/{fileName}")]
    Task<Stream> GetFileAsync(string fileName, CancellationToken cancellationToken = default);
}
