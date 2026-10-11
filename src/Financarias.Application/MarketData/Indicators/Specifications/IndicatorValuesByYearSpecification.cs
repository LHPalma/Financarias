using Ardalis.Specification;
using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.Specifications;

public sealed class IndicatorValuesByYearSpecification : Specification<IndicatorValue>
{
    public IndicatorValuesByYearSpecification(EconomicIndicator indicator, int year)
    {
        var firstDay = new DateOnly(year, 1, 1);
        var lastDay = new DateOnly(year, 12, 31);

        Query.Where(value =>
            value.Indicator == indicator && value.ReferenceDate >= firstDay && value.ReferenceDate <= lastDay);
    }
}
