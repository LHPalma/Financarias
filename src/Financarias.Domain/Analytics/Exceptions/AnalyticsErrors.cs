using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Analytics.Exceptions;

public static class AnalyticsErrors
{
    public const string BusinessDayCountInvalid = "analytics.businessdaycount.invalid";
    public const string InstallmentCountInvalid = "analytics.installmentcount.invalid";
    public const string MonthlyRateInvalid = "analytics.monthlyrate.invalid";
    public const string NominalValueInvalid = "analytics.nominalvalue.invalid";
    public const string PayoffPeriodInvalid = "analytics.financing.payoffperiod.invalid";
    public const string UnrepresentableFinancing = "analytics.financing.unrepresentable";
    public const string YieldInvalid = "analytics.yield.invalid";

    public static DomainValidationException BusinessDayCount(int value) =>
        new(BusinessDayCountInvalid, $"Business day count not valid: '{value}'. Must be zero or greater.");

    public static DomainValidationException InstallmentCount(int value) =>
        new(InstallmentCountInvalid, $"Installment count not valid: '{value}'. Must be between 1 and 600.");

    public static DomainValidationException MonthlyRate(decimal value) =>
        new(MonthlyRateInvalid, $"Monthly rate not valid: '{value}'. Must be zero or greater.");

    public static DomainValidationException NominalValue(decimal value) =>
        new(NominalValueInvalid, $"Nominal value not valid: '{value}'. Must be greater than zero.");

    public static DomainValidationException PayoffPeriod(int period, int installments) =>
        new(PayoffPeriodInvalid, $"Payoff period not valid: '{period}'. Must be between 1 and {installments}.");

    public static DomainValidationException Yield(decimal value) =>
        new(YieldInvalid, $"Annual yield not valid: '{value}'. Must be greater than -1 (i.e. > -100%).");
}
