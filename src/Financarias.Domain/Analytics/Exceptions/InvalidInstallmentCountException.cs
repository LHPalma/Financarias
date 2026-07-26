using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Analytics.Exceptions;

public sealed class InvalidInstallmentCountException(int value)
    : BaseDomainException(
        "analytics.installmentcount.invalid",
        $"Installment count not valid: '{value}'. Must be between 1 and 600.");