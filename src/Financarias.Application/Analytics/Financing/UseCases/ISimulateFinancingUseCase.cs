using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;

namespace Financarias.Application.Analytics.Financing.UseCases;

/// <summary>
/// Simula um financiamento pela Tabela Price a partir do <see cref="SimulateFinancingRequest"/>:
/// valida os campos em VOs, calcula a parcela fixa e devolve a tabela de evolução do saldo devedor.
/// Taxa mensal em fração (0.015 = 1,5% a.m.).
/// </summary>
public interface ISimulateFinancingUseCase
{
    Task<FinancingSimulationResult> ExecuteAsync(
        SimulateFinancingRequest request,
        CancellationToken cancellationToken = default);
}
