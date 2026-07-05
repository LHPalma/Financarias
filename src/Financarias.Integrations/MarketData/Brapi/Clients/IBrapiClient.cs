using Financarias.Integrations.MarketData.Brapi.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.MarketData.Brapi.Clients;

public interface IBrapiClient
{
    [Get("/quote/{ticker}")]
    Task<BrapiQuoteResponse> GetQuoteAsync(string ticker, CancellationToken cancellationToken = default);
}