using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Domain.MarketData.Fuel;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IQueryable<BrandAveragePriceResult>> AveragePricesByBrand(
        FuelProduct product, string? state = null, CancellationToken cancellationToken = default)
    {
        // Filtro/ordenação do Hot Chocolate sobre AveragePrice/StationCount não traduz pro SQL — o EF
        // não consegue recompor o Average()/Count() de origem por trás do campo já projetado. O
        // resultado pós-agregado é sempre pequeno (poucas dezenas de linhas), então materializa aqui e
        // deixa o Hot Chocolate filtrar/ordenar em memória sobre a lista pronta.
        var results = await LatestPricesByProduct(product)
            .Where(p => state == null || p.FuelStation.State == state)
            .GroupBy(p => new { p.FuelStation.Brand, p.FuelStation.State })
            .Select(g => new BrandAveragePriceResult(
                Brand: g.Key.Brand,
                State: g.Key.State,
                AveragePrice: g.Average(p => p.SalePrice),
                StationCount: g.Count()))
            .ToListAsync(cancellationToken);

        return results.AsQueryable();
    }
}
