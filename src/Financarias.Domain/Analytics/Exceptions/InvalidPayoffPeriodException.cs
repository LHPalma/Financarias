using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Analytics.Exceptions;

public sealed class InvalidPayoffPeriodException(int period, int installments)
    : BaseDomainException(
        "analytics.financing.payoffperiod.invalid",
        $"Payoff period not valid: '{period}'. Must be between 1 and {installments}.");
