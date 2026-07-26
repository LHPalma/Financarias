using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Analytics.Exceptions;

public sealed class InvalidMonthlyRateException(decimal value)
    : BaseDomainException(
        "analytics.monthlyrate.invalid",
        $"Monthly rate not valid: '{value}'. Must be zero or greater.");