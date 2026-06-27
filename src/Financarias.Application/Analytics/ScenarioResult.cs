namespace Financarias.Application.Analytics;

public sealed record ScenarioResult(
    decimal BuyPrice,
    decimal SellPrice,
    decimal GrossProfit,
    decimal Profitability);