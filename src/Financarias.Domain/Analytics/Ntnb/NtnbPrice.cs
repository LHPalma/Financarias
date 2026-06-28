namespace Financarias.Domain.Analytics.Ntnb;

public record NtnbPrice(
    DateOnly SettlementDate,
    decimal ProjectedVna,
    BusinessDayCount BusinessDaysToMaturity,
    decimal UnitPrice);