using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class FinancingQueries
{
    [GraphQLName("simulateFinancing")]
    public Task<FinancingSimulationResult> SimulateFinancingAsync(
        SimulateFinancingRequest input,
        ISimulateFinancingUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    [GraphQLName("simulateEarlyPayoff")]
    public Task<EarlyPayoffResult> SimulateEarlyPayoffAsync(
        SimulateEarlyPayoffRequest input,
        ISimulateEarlyPayoffUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
