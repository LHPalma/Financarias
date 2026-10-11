using Ardalis.Specification;
using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.Specifications;

public sealed class IndicatorValuesByIndicatorSpecification : Specification<IndicatorValue>
{
    public IndicatorValuesByIndicatorSpecification(EconomicIndicator indicator)
    {
        Query
            .Where(value => value.Indicator == indicator)
            .OrderBy(value => value.ReferenceDate);
    }
}
