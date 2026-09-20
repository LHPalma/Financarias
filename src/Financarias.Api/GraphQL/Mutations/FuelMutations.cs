using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Fuel.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class FuelMutations
{
    [GraphQLName("importFuelPrices")]
    public Task<FuelImportResult> ImportFuelPricesAsync(
        IImportFuelPricesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}
