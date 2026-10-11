using Ardalis.Specification;
using Financarias.Domain.MarketData.CorporateEvents;

namespace Financarias.Application.MarketData.CorporateEvents.Specifications;

public sealed class CorporateEventsByIssuerSpecification : Specification<CorporateEvent>
{
    public CorporateEventsByIssuerSpecification(string issuerCode)
    {
        Query.Where(corporateEvent => corporateEvent.IssuerCode == issuerCode);
    }
}
