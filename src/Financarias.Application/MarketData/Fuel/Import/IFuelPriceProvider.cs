namespace Financarias.Application.MarketData.Fuel.Import;

/// <summary>
/// Porta de origem dos preços de combustível para importação: transmite (streaming) os itens já
/// parseados de um provedor externo (ex.: arquivo semestral da ANP), um a um, para não carregar as
/// centenas de milhares de linhas na memória de uma vez. O adapter pula linhas malformadas
/// (dado ausente/ inválido = miss, não erro); falha de transporte propaga.
/// </summary>
public interface IFuelPriceProvider
{
    IAsyncEnumerable<FuelPriceImportItem> FetchFuelPricesAsync(CancellationToken cancellationToken = default);
}
