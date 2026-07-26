namespace Financarias.Application.Analytics.Financing.DTOs.Results;

public record EarlyPayoffResult(
    int Period,
    decimal OutstandingBalance,
    int InstallmentsRemaining,
    decimal InterestPaid,
    decimal InterestSaved);
