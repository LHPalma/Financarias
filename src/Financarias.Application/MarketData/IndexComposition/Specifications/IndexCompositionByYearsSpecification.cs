using Ardalis.Specification;
using Financarias.Domain.MarketData.IndexComposition;

namespace Financarias.Application.MarketData.IndexComposition.Specifications;

public sealed class IndexCompositionByYearsSpecification : Specification<IndexCompositionItem>
{
    public IndexCompositionByYearsSpecification(IReadOnlyCollection<int> years)
    {
        Query.Where(item => years.Contains(item.Year));
    }
}
