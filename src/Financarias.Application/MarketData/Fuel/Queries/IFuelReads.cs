using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Application.MarketData.Fuel.Queries;

public interface IFuelReads
{
    IQueryable<FuelPrice> LatestPricesByProduct(FuelProduct product);

    Task<IQueryable<BrandAveragePriceResult>> AveragePricesByBrand(
        FuelProduct product, string? state = null, CancellationToken cancellationToken = default);

    Task<IQueryable<StateAveragePriceResult>> AveragePriceByState(
        FuelProduct product, CancellationToken cancellationToken = default);

    Task<IQueryable<MunicipalityAveragePriceResult>> AveragePriceByMunicipality(
        FuelProduct product, string? state = null, CancellationToken cancellationToken = default);
}
