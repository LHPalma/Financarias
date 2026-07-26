using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Application.Common.Messaging;
using Financarias.Domain.Analytics.Financing;

namespace Financarias.Application.Analytics.Financing.Queries;

public class SimulateFinancingQueryHandler(
    SimulateFinancingMapper mapper
) : IQueryHandler<SimulateFinancingQuery, FinancingSimulationResult>
{
    public Task<FinancingSimulationResult> HandleAsync(
        SimulateFinancingQuery query,
        CancellationToken cancellationToken = default)
    {
        var schedule = FrenchAmortization.Schedule(query.Principal, query.MonthlyRate, query.Installments);

        return Task.FromResult(mapper.ToResult(schedule));
    }
}
