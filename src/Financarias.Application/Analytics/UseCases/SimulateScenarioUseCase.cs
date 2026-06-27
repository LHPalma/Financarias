using Financarias.Application.Analytics.Queries;
using Financarias.Application.Common.Messaging;
using Financarias.Domain.Analytics;

namespace Financarias.Application.Analytics.UseCases;

public sealed class SimulateScenarioUseCase(
    IQueryHandler<SimulateScenarioQuery, ScenarioResult> handler
) : ISimulateScenarioUseCase
{
    public Task<ScenarioResult> ExecuteAsync(
        decimal vna,
        decimal buyYield,
        decimal sellYield,
        int businessDays,
        CancellationToken cancellationToken = default)
    {
        var query = new SimulateScenarioQuery(
            NominalValue.Create(vna),
            AnnualYield.FromFraction(buyYield),
            AnnualYield.FromFraction(sellYield),
            BusinessDayCount.Create(businessDays));

        return handler.HandleAsync(query, cancellationToken);
    }
}
