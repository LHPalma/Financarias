namespace Financarias.Application.Analytics.Financing.DTOs.Results;

/// <summary>Resultado da simulação de um financiamento pela Tabela Price.</summary>
/// <param name="Installment">Valor da parcela fixa, em reais. A última parcela absorve a diferença de centavos do arredondamento.</param>
/// <param name="TotalPaid">Soma de todas as parcelas.</param>
/// <param name="TotalInterest">Total de juros pagos ao longo do financiamento.</param>
/// <param name="Schedule">Tabela de evolução, uma linha por parcela, em ordem.</param>
public record FinancingSimulationResult(
    decimal Installment,
    decimal TotalPaid,
    decimal TotalInterest,
    IReadOnlyList<InstallmentBreakdownResult> Schedule);
