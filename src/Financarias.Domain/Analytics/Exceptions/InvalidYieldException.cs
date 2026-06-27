using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Analytics.Exceptions;

public sealed class InvalidYieldException(decimal value)
    : BaseDomainException("analytics.yield.invalid", $"Annual yield not valid: '{value}'. Must be greater than -1 (i.e. > -100%).");