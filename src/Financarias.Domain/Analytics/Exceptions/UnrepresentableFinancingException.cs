using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Analytics.Exceptions;

public sealed class UnrepresentableFinancingException(decimal rate, int installments)
    : BaseDomainException(
        "analytics.financing.unrepresentable",
        $"Financing not representable: rate '{rate}' over {installments} instalments exceeds decimal range.");