using Financarias.Domain.Common;

namespace Financarias.Domain.MarketData.Indicators;

public sealed class IndicatorValue : BaseEntity<long>, IAggregateRoot
{
    public const int FirstYear = 1980;

    private IndicatorValue()
    {
    }

    private IndicatorValue(EconomicIndicator indicator, DateOnly referenceDate, decimal value)
    {
        Indicator = indicator;
        ReferenceDate = referenceDate;
        Value = value;
    }

    public EconomicIndicator Indicator { get; private set; }

    public DateOnly ReferenceDate { get; private set; }

    public decimal Value { get; private set; }

    public static IndicatorValue Create(EconomicIndicator indicator, DateOnly referenceDate, decimal value) =>
        new(indicator, referenceDate, value);

    public void UpdateValue(decimal value) => Value = value;
}
