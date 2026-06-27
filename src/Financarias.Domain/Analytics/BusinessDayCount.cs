using System.Globalization;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.Analytics;

public sealed record BusinessDayCount
{
    private BusinessDayCount(int value) => Value = value;

    public int Value { get; }

    public static BusinessDayCount Create(int value)
    {
        return value >= 0 ? new BusinessDayCount(value) : throw new InvalidBusinessDayCountException(value);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}