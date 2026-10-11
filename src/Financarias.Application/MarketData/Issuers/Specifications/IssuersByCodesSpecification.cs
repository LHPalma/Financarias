using Ardalis.Specification;
using Financarias.Domain.MarketData.Issuers;

namespace Financarias.Application.MarketData.Issuers.Specifications;

public sealed class IssuersByCodesSpecification : Specification<Issuer>
{
    public IssuersByCodesSpecification(IReadOnlyCollection<string> issuerCodes)
    {
        Query.Where(issuer => issuerCodes.Contains(issuer.IssuerCode));
    }
}
