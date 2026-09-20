namespace Financarias.Application.Analytics.DTOs.Results;

/// <summary>Preço de uma NTN-B Principal calculado por datas.</summary>
/// <param name="SettlementDate">Data de liquidação: a data de negociação mais 2 dias úteis (T+2).</param>
/// <param name="ProjectedVna">VNA projetado para a data de liquidação, pelo pró-rata do IPCA projetado, truncado em 6 casas.</param>
/// <param name="BusinessDaysToMaturity">Dias úteis entre a liquidação (inclusive) e o vencimento (exclusive).</param>
/// <param name="UnitPrice">Preço unitário (PU) do título, em reais, truncado em 6 casas.</param>
public record NtnbPriceResult(
    DateOnly SettlementDate,
    decimal ProjectedVna,
    int BusinessDaysToMaturity,
    decimal UnitPrice);