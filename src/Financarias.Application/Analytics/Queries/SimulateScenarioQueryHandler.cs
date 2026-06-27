using Financarias.Application.Common.Messaging;
using Financarias.Domain.Analytics;

namespace Financarias.Application.Analytics.Queries;

public sealed class SimulateScenarioQueryHandler
    : IQueryHandler<SimulateScenarioQuery, ScenarioResult>
{
    public Task<ScenarioResult> HandleAsync(
        SimulateScenarioQuery query,
        CancellationToken cancellationToken = default)
    {
        var buyPrice = NtnbPricing.UnitPrice(query.Vna.Value, query.BuyYield, query.BusinessDays);
        var sellPrice = NtnbPricing.UnitPrice(query.Vna.Value, query.SellYield, query.BusinessDays);

        var grossProfit = sellPrice - buyPrice;
        var profitability = decimal.Round(grossProfit / buyPrice, 4, MidpointRounding.AwayFromZero) * 100m;

        return Task.FromResult(new ScenarioResult(buyPrice, sellPrice, grossProfit, profitability));
    }
}
