using Refit;

namespace Financarias.Integrations.MarketData.Google.Trends.Clients;

public interface IGoogleTrendsClient
{
    [Get("/trends/?geo={geo}")]
    Task<string> OpenSessionAsync(string geo, CancellationToken cancellationToken = default);

    [Get("/trends/api/explore")]
    Task<string> ExploreAsync(
        [AliasAs("hl")] string language,
        [AliasAs("tz")] int timezone,
        [AliasAs("req")] string request,
        CancellationToken cancellationToken = default);

    [Get("/trends/api/widgetdata/multiline")]
    Task<string> GetTimelineAsync(
        [AliasAs("hl")] string language,
        [AliasAs("tz")] int timezone,
        [AliasAs("req")] string request,
        [AliasAs("token")] string token,
        CancellationToken cancellationToken = default);
}
