using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Application.MarketData.Fuel.UseCases;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class FuelQueries
{
    /// <summary>
    ///     Compara, por posto, o preço mais recente coletado do etanol com o da gasolina comum e aplica a regra dos 70%: o etanol compensa quando custa menos de 70% do preço da gasolina. Postos sem preço de um dos dois produtos ficam de fora. Não exige autenticação.
    ///
    ///     Aceita paginação, filtro (`where`) e ordenação (`order`). Hoje o filtro e a ordenação rodam em memória sobre a lista inteira, e não no banco.
    /// </summary>
    [GraphQLName("ethanolGasolineParity")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<EthanolGasolineParityResult>> GetEthanolGasolineParityAsync(
        IFindEthanolGasolineParityUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);

    /// <summary>
    ///     Preços mais recentes de um combustível: a última coleta de cada posto. Para ver os mais baratos primeiro, ordene por `salePrice` crescente. Não exige autenticação.
    ///
    ///     Aceita paginação, filtro (`where`) e ordenação (`order`). O filtro e a ordenação são executados no banco.
    /// </summary>
    /// <param name="product">Combustível.</param>
    [GraphQLName("cheapestFuelPrices")]
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<FuelPrice> GetCheapestFuelPricesAsync(FuelProduct product, IFuelReads reads)
        => reads.LatestPricesByProduct(product);

    /// <summary>
    ///     Preço médio de venda por bandeira e estado sobre a última coleta de cada posto, com a quantidade de postos considerados. Não exige autenticação.
    ///
    ///     Aceita paginação, filtro (`where`) e ordenação (`order`). Filtro e ordenação rodam em memória, sobre o resultado já agregado, que é pequeno.
    /// </summary>
    /// <param name="product">Combustível.</param>
    /// <param name="state">Sigla da UF em maiúsculas, como SP, para restringir o resultado a um estado. Sem ela, considera todos.</param>
    [GraphQLName("averagePricesByBrand")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<BrandAveragePriceResult>> GetAveragePricesByBrandAsync(
        FuelProduct product, string? state, IFuelReads reads, CancellationToken cancellationToken) =>
        reads.AveragePricesByBrand(product, state, cancellationToken);

    /// <summary>
    ///     Preço médio de venda por estado sobre a última coleta de cada posto, com a quantidade de postos considerados. Não exige autenticação.
    ///
    ///     Aceita paginação, filtro (`where`) e ordenação (`order`). Filtro e ordenação rodam em memória, sobre o resultado já agregado, que é pequeno.
    /// </summary>
    /// <param name="product">Combustível.</param>
    [GraphQLName("averagePriceByState")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<StateAveragePriceResult>> GetAveragePriceByStateAsync(
        FuelProduct product, IFuelReads reads, CancellationToken cancellationToken) =>
        reads.AveragePriceByState(product, cancellationToken);

    /// <summary>
    ///     Preço médio de venda por município sobre a última coleta de cada posto, com a quantidade de postos considerados. Não exige autenticação.
    ///
    ///     Aceita paginação, filtro (`where`) e ordenação (`order`). Filtro e ordenação rodam em memória, sobre o resultado já agregado, que é pequeno.
    /// </summary>
    /// <param name="product">Combustível.</param>
    /// <param name="state">Sigla da UF em maiúsculas, como SP, para restringir o resultado a um estado. Sem ela, considera todos.</param>
    [GraphQLName("averagePriceByMunicipality")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<MunicipalityAveragePriceResult>> GetAveragePriceByMunicipalityAsync(
        FuelProduct product, string? state, IFuelReads reads, CancellationToken cancellationToken) =>
        reads.AveragePriceByMunicipality(product, state, cancellationToken);
}
