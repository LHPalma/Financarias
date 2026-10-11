using Financarias.Integrations.MarketData.Fred.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.MarketData.Fred.Clients;

public interface IFredClient
{
    [Get("/fred/series/observations?file_type=json")]
    Task<FredObservationsResponse> GetObservationsAsync(
        [AliasAs("series_id")] string series,
        [AliasAs("observation_start")] string startDate,
        [AliasAs("observation_end")] string endDate,
        [AliasAs("api_key")] string apiKey,
        CancellationToken cancellationToken = default);
}
