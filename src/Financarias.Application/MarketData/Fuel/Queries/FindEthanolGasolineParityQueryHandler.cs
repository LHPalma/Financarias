using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Domain.MarketData.Fuel;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Application.MarketData.Fuel.Queries;

public sealed class FindEthanolGasolineParityQueryHandler(
    IApplicationDbContext dbContext
) : IQueryHandler<FindEthanolGasolineParityQuery, IReadOnlyList<EthanolGasolineParityResult>>
{
    public async Task<IReadOnlyList<EthanolGasolineParityResult>> HandleAsync(
        FindEthanolGasolineParityQuery query,
        CancellationToken cancellationToken = default)
    {
        var prices = await dbContext.FuelPrices
            .Where(p => p.Product == FuelProduct.Ethanol || p.Product == FuelProduct.Gasoline)
            .Include(p => p.FuelStation)
            .ToListAsync(cancellationToken);

        var pairs = prices
            .GroupBy(p => p.StationId)
            .Select(g => new
            {
                Station = g.First().FuelStation,
                Ethanol = g.Where(p => p.Product == FuelProduct.Ethanol)
                    .OrderByDescending(p => p.CollectedOn).FirstOrDefault(),
                Gasoline = g.Where(p => p.Product == FuelProduct.Gasoline)
                    .OrderByDescending(p => p.CollectedOn).FirstOrDefault(),
            })
            .Where(pair => pair.Ethanol is not null && pair.Gasoline is not null);

        return [.. pairs
            .Select(pair =>
            {
                var ratio = EthanolGasolineParity.Ratio(pair.Ethanol!.SalePrice, pair.Gasoline!.SalePrice);
                return new EthanolGasolineParityResult(
                    StationName: pair.Station.Name,
                    Brand: pair.Station.Brand,
                    Municipality: pair.Station.Municipality,
                    State: pair.Station.State,
                    EthanolPrice: pair.Ethanol.SalePrice,
                    GasolinePrice: pair.Gasoline.SalePrice,
                    Ratio: ratio,
                    IsEthanolAdvantageous: EthanolGasolineParity.IsEthanolAdvantageous(ratio));
            })];
    }
}