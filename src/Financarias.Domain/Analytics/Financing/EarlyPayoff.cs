namespace Financarias.Domain.Analytics.Financing;

public record EarlyPayoff(
    int Period,
    decimal OutstandingBalance,
    int InstallmentsRemaining,
    decimal InterestPaid,
    decimal InterestSaved);
