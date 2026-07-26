namespace Financarias.Application.Analytics.Financing.DTOs.Results;

public record InstallmentBreakdownResult(
    int Period,
    decimal Installment,
    decimal Interest,
    decimal Amortization,
    decimal OutstandingBalance);
