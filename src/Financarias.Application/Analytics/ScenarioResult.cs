namespace Financarias.Application.Analytics;

/// <summary>Resultado da simulação de compra e venda de uma NTN-B em duas taxas.</summary>
/// <param name="BuyPrice">PU de compra, calculado na taxa de compra.</param>
/// <param name="SellPrice">PU de venda, calculado na taxa de venda.</param>
/// <param name="GrossProfit">Lucro bruto por título: PU de venda menos PU de compra.</param>
/// <param name="Profitability">Rentabilidade do cenário em percentual (5,23 significa 5,23%): o lucro bruto sobre o PU de compra.</param>
public sealed record ScenarioResult(
    decimal BuyPrice,
    decimal SellPrice,
    decimal GrossProfit,
    decimal Profitability);