using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.Import;

/// <summary>
/// Porta de origem dos valores de um indicador econômico para importação: obtém, de um provedor externo
/// (ex.: séries do SGS do Banco Central), todos os valores de um ano. Ano sem dados devolve lista vazia;
/// falha de transporte propaga.
/// </summary>
public interface IIndicatorValueProvider
{
    Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default);
}
