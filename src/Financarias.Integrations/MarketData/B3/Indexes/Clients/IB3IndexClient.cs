using Financarias.Integrations.MarketData.B3.Indexes.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.MarketData.B3.Indexes.Clients;

public interface IB3IndexClient
{
    [Get("/indexStatisticsProxy/IndexCall/GetPortfolioDay/{payload}")]
    Task<B3IndexStatisticsResponse> GetYearAsync(string payload, CancellationToken cancellationToken = default);
}
