using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Application.Analytics.Financing.Queries;
using Financarias.Application.Common.Messaging;

namespace Financarias.Application.Analytics.Financing.UseCases;

public sealed class SimulateFinancingUseCase(
    SimulateFinancingMapper mapper,
    IQueryHandler<SimulateFinancingQuery, FinancingSimulationResult> handler
) : ISimulateFinancingUseCase
{
    public Task<FinancingSimulationResult> ExecuteAsync(
        SimulateFinancingRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = mapper.ToQuery(request);
        return handler.HandleAsync(query, cancellationToken);
    }
}
