namespace Financarias.Domain.Analytics.Financing;

public record InstallmentRow(
    int Period,
    decimal Installment,
    decimal Interest,
    decimal AccumulatedInterest,
    decimal Amortization,
    decimal OutstandingBalance);