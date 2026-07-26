namespace Financarias.Application.Analytics.Financing.DTOs.Results;

public record FinancingSimulationResult(
    decimal Installment,
    decimal TotalPaid,
    decimal TotalInterest,
    IReadOnlyList<InstallmentBreakdownResult> Schedule);
