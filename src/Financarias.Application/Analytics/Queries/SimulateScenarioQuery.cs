using Financarias.Application.Common.Messaging;
using Financarias.Domain.Analytics;

namespace Financarias.Application.Analytics.Queries;

public sealed record SimulateScenarioQuery(
    NominalValue Vna,
    AnnualYield BuyYield,
    AnnualYield SellYield,
    BusinessDayCount BusinessDays
) : IQuery<ScenarioResult>;
