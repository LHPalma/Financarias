using Ardalis.Specification;
using Financarias.Domain.MarketData.SearchInterest;

namespace Financarias.Application.MarketData.SearchInterest.Specifications;

public sealed class SearchInterestByGroupSpecification : Specification<SearchInterestPoint>
{
    public SearchInterestByGroupSpecification(string geo, string comparisonKey, DateOnly rangeStart, DateOnly rangeEnd)
    {
        Query.Where(point =>
            point.Geo == geo
            && point.ComparisonKey == comparisonKey
            && point.RangeStart == rangeStart
            && point.RangeEnd == rangeEnd);
    }
}
