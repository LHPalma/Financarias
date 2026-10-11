using Financarias.Application.MarketData.Inflation.Commands;

namespace Financarias.Application.MarketData.Inflation.UseCases;

public interface IRebuildInflationLevelsUseCase
{
    /// <summary>
    /// Recalcula o índice de inflação diário a partir do IPCA mensal e do calendário de feriados, pela
    /// metodologia da B3, e deixa gravada só a diferença; lança quando o IPCA não existe ou tem buraco.
    /// </summary>
    Task<InflationLevelsRebuildResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
