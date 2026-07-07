using Ardalis.Specification;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Application.MarketData.Fuel.Specifications;

public sealed class StationsByCnpjsSpecification : Specification<FuelStation>
{
    public StationsByCnpjsSpecification(IReadOnlyCollection<Cnpj> cnpjs)
    {
        Query.Where(station => cnpjs.Contains(station.Cnpj));
    }
}
