using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Application.MarketData.Fuel.Queries;

public interface IFuelReads
{
    IQueryable<FuelPrice> LatestPricesByProduct(FuelProduct product);

    IQueryable<BrandAveragePriceResult> AveragePricesByBrand(FuelProduct product, string? state = null);
}