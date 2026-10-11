namespace Financarias.Application.MarketData.Indicators.Import;

/// <summary>Resultado da importação dos valores de um indicador econômico.</summary>
/// <param name="RowsRead">Valores lidos do Banco Central e processados. Zero para um ano sem dados, como um ano anterior ao início da série.</param>
/// <param name="ValuesCreated">Valores novos inseridos.</param>
/// <param name="ValuesUpdated">Valores já existentes (mesmo indicador e data) cujo número mudou e foi atualizado.</param>
public sealed record IndicatorValueImportResult(
    int RowsRead,
    int ValuesCreated,
    int ValuesUpdated);
