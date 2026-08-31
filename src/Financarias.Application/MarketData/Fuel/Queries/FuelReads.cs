using Financarias.Application.Common.Persistence;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Application.MarketData.Fuel.Queries;

public class FuelReads(IApplicationDbContext dbContext) : IFuelReads
{
    public IQueryable<FuelPrice> LatestPricesByProduct(FuelProduct product)
    {
        return dbContext.FuelPrices
            .Where(p => p.Product == product)
            .Where(p => p.CollectedOn ==
                dbContext.FuelPrices
                    .Where(p2 => p2.StationId == p.StationId && p2.Product == product)
                    .Max(p2 => p2.CollectedOn)
            );
    }
}