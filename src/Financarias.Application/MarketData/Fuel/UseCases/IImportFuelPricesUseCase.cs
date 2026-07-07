using Financarias.Application.MarketData.Fuel.Import;

namespace Financarias.Application.MarketData.Fuel.UseCases;

/// <summary>
/// Importa os preços de combustível da ANP (arquivo semestral) e persiste postos e coletas via
/// upsert por chave natural (posto = CNPJ; preço = posto + produto + data da coleta), devolvendo
/// os totais de linhas processadas, postos criados e preços inseridos/atualizados.
/// </summary>
public interface IImportFuelPricesUseCase
{
    Task<FuelImportResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
