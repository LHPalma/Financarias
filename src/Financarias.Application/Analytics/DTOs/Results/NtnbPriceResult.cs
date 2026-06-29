namespace Financarias.Application.Analytics.DTOs.Results;

public record NtnbPriceResult(
    DateOnly SettlementDate,
    decimal ProjectedVna,
    int BusinessDaysToMaturity,
    decimal UnitPrice);