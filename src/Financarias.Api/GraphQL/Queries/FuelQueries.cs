using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Application.MarketData.Fuel.UseCases;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class FuelQueries
{
    [GraphQLName("ethanolGasolineParity")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<EthanolGasolineParityResult>> GetEthanolGasolineParityAsync(
        IFindEthanolGasolineParityUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);

    [GraphQLName("cheapestFuelPrices")]
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<FuelPrice> GetCheapestFuelPricesAsync(FuelProduct product, IFuelReads reads)
        => reads.LatestPricesByProduct(product);

    [GraphQLName("averagePricesByBrand")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<BrandAveragePriceResult>> GetAveragePricesByBrandAsync(
        FuelProduct product, string? state, IFuelReads reads, CancellationToken cancellationToken) =>
        reads.AveragePricesByBrand(product, state, cancellationToken);

    [GraphQLName("averagePriceByState")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<StateAveragePriceResult>> GetAveragePriceByStateAsync(
        FuelProduct product, IFuelReads reads, CancellationToken cancellationToken) =>
        reads.AveragePriceByState(product, cancellationToken);

    [GraphQLName("averagePriceByMunicipality")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<MunicipalityAveragePriceResult>> GetAveragePriceByMunicipalityAsync(
        FuelProduct product, string? state, IFuelReads reads, CancellationToken cancellationToken) =>
        reads.AveragePriceByMunicipality(product, state, cancellationToken);
}
