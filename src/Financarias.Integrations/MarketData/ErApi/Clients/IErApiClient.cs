using Financarias.Integrations.MarketData.ErApi.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.MarketData.ErApi.Clients;

public interface IErApiClient
{
    [Get("/v6/latest/{baseCode}")]
    Task<ErApiLatestResponse> GetLatestAsync(string baseCode, CancellationToken cancellationToken = default);
}
