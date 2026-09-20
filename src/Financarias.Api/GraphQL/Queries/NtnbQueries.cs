using Financarias.Application.Analytics;
using Financarias.Application.Analytics.DTOs.Requests;
using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Analytics.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class NtnbQueries
{
    [GraphQLName("simulateNtnbScenario")]
    public Task<ScenarioResult> SimulateNtnbScenarioAsync(
        decimal vna,
        decimal buyYield,
        decimal sellYield,
        int businessDays,
        ISimulateScenarioUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(vna, buyYield, sellYield, businessDays, cancellationToken);

    [GraphQLName("calculateNtnbPrice")]
    public Task<NtnbPriceResult> CalculateNtnbPriceAsync(
        CalculateNtnbPriceRequest input,
        ICalculateNtnbPriceUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
