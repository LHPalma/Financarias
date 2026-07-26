namespace Financarias.Domain.Analytics.Financing;

public record AmortizationSchedule(
    decimal Installment,
    decimal TotalPaid,
    decimal TotalInterest,
    IReadOnlyList<InstallmentRow> Rows);