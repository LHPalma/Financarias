using Refit;

namespace Financarias.Integrations.MarketData.Anp.Fuel.Clients;

public interface IAnpFuelClient
{
    [Get("/anp/pt-br/centrais-de-conteudo/dados-abertos/arquivos/shpc/dsas/ca/ca-{fileId}.zip")]
    Task<Stream> GetFuelPricesFileAsync(string fileId, CancellationToken cancellationToken = default);
}
