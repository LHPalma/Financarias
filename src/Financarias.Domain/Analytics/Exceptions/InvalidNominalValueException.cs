using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Analytics.Exceptions;

public sealed class InvalidNominalValueException(decimal value)
    : BaseDomainException(
        "analytics.nominalvalue.invalid",
        $"Nominal value not valid: '{value}'. Must be greater than zero.");
