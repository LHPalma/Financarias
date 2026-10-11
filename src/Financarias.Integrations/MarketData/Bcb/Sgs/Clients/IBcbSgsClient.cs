using Financarias.Integrations.MarketData.Bcb.Sgs.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.MarketData.Bcb.Sgs.Clients;

public interface IBcbSgsClient
{
    [Get("/dados/serie/bcdata.sgs.{series}/dados?formato=json")]
    Task<List<BcbSgsPoint>> GetSeriesAsync(
        int series,
        [AliasAs("dataInicial")] string startDate,
        [AliasAs("dataFinal")] string endDate,
        CancellationToken cancellationToken = default);
}
