using Financarias.Domain.Analytics;

namespace Financarias.Application.Analytics.UseCases;

/// <summary>
/// Caso de uso de simulação de cenário de NTN-B: dado o VNA, as taxas de compra e venda simuladas
/// e os dias úteis até o vencimento, calcula PU de compra, PU de venda, lucro e rentabilidade.
/// As taxas entram em forma fracionária (0.07 = 7%).
/// </summary>
public interface ISimulateScenarioUseCase
{
    Task<ScenarioResult> ExecuteAsync(
        decimal vna,
        decimal buyYield,
        decimal sellYield,
        int businessDays,
        CancellationToken cancellationToken = default);
}
