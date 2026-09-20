namespace Financarias.Application.MarketData.Fuel.Import;

/// <summary>Resultado da importação de preços de combustível da ANP.</summary>
/// <param name="RowsProcessed">Linhas do arquivo processadas.</param>
/// <param name="StationsCreated">Postos novos criados.</param>
/// <param name="PricesCreated">Preços novos inseridos.</param>
/// <param name="PricesUpdated">Preços já existentes (mesmo posto, produto e data de coleta) que foram atualizados.</param>
public sealed record FuelImportResult(
    int RowsProcessed,
    int StationsCreated,
    int PricesCreated,
    int PricesUpdated);
