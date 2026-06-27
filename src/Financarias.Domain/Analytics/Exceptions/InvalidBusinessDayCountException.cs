using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Analytics.Exceptions;

public sealed class InvalidBusinessDayCountException(int value)
    : BaseDomainException(
        "analytics.businessdaycount.invalid",
        $"Business day count not valid: '{value}'. Must be zero or greater.");