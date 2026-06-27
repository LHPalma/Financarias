using System.Globalization;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.Analytics;

/// <summary>Taxa anual de rendimento em forma fracionária (0.07 = 7% a.a.).</summary>
public sealed record AnnualYield
{
    private AnnualYield(decimal value) => Value = value;

    public decimal Value { get; }

    public static AnnualYield FromFraction(decimal value)
    {
        return value > -1m ? new AnnualYield(value) : throw new InvalidYieldException(value);
    }

    public static AnnualYield FromPercent(decimal percent) => FromFraction(percent / 100m);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}