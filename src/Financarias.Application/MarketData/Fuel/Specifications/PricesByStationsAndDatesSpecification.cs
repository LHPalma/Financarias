using Ardalis.Specification;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Application.MarketData.Fuel.Specifications;

public sealed class PricesByStationsAndDatesSpecification : Specification<FuelPrice>
{
    public PricesByStationsAndDatesSpecification(
        IReadOnlyCollection<long> stationIds,
        IReadOnlyCollection<DateOnly> collectedDates)
    {
        Query.Where(price => stationIds.Contains(price.StationId) && collectedDates.Contains(price.CollectedOn));
    }
}
