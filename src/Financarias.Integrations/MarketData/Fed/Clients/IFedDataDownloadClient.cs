using Refit;

namespace Financarias.Integrations.MarketData.Fed.Clients;

public interface IFedDataDownloadClient
{
    [Get("/datadownload/Output.aspx?rel=H10&filetype=csv&label=include&layout=seriescolumn")]
    Task<string> GetSeriesAsync(
        string series,
        string from,
        string to,
        CancellationToken cancellationToken = default);
}
