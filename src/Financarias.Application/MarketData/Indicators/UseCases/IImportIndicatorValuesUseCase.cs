using Financarias.Application.MarketData.Indicators.DTOs.Requests;
using Financarias.Application.MarketData.Indicators.Import;

namespace Financarias.Application.MarketData.Indicators.UseCases;

public interface IImportIndicatorValuesUseCase
{
    /// <summary>
    /// Importa do Banco Central os valores de um indicador econômico em um ano e persiste por upsert; lança
    /// quando o ano é anterior ao primeiro ano aceito.
    /// </summary>
    Task<IndicatorValueImportResult> ExecuteAsync(
        ImportIndicatorValuesRequest request,
        CancellationToken cancellationToken = default);
}
