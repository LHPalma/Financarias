using Financarias.Domain.Common;

namespace Financarias.Domain.MarketData.Inflation;

public sealed class InflationLevel : BaseEntity<long>, IAggregateRoot
{
    private InflationLevel()
    {
    }

    private InflationLevel(DateOnly day, decimal level)
    {
        EnsureValid(level);

        Day = day;
        Level = level;
    }

    public DateOnly Day { get; private set; }

    public decimal Level { get; private set; }

    public static InflationLevel Create(DateOnly day, decimal level) => new(day, level);

    public void UpdateLevel(decimal level)
    {
        EnsureValid(level);

        Level = level;
    }

    private static void EnsureValid(decimal level)
    {
        if (level <= 0)
        {
            throw MarketDataErrors.InflationSeries($"level must be positive, got {level}");
        }
    }
}
