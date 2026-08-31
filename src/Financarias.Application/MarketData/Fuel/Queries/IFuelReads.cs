using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Application.MarketData.Fuel.Queries;

public interface IFuelReads
{
    IQueryable<FuelPrice> LatestPricesByProduct(FuelProduct product);
}