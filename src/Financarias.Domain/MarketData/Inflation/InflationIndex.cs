using Financarias.Domain.Calendar;
using Financarias.Domain.MarketData.Currencies;

namespace Financarias.Domain.MarketData.Inflation;

public sealed class InflationIndex
{
    private readonly IReadOnlySet<DateOnly> _holidays;
    private readonly Dictionary<int, decimal> _monthlyFactor;
    private readonly Dictionary<int, decimal> _levelAtWindowStart;
    private readonly int _firstKey;
    private readonly int _lastKey;

    private InflationIndex(
        IReadOnlySet<DateOnly> holidays,
        Dictionary<int, decimal> monthlyFactor,
        Dictionary<int, decimal> levelAtWindowStart,
        int firstKey,
        int lastKey)
    {
        _holidays = holidays;
        _monthlyFactor = monthlyFactor;
        _levelAtWindowStart = levelAtWindowStart;
        _firstKey = firstKey;
        _lastKey = lastKey;
        FirstDate = AdjustedFifteenth(firstKey);
        LastDate = AdjustedFifteenth(lastKey + 1);
    }

    public DateOnly FirstDate { get; }

    public DateOnly LastDate { get; }

    public static InflationIndex FromMonthlyRates(
        IEnumerable<(DateOnly Month, decimal RatePercent)> monthlyRates,
        IReadOnlySet<DateOnly> holidays)
    {
        ArgumentNullException.ThrowIfNull(monthlyRates);
        ArgumentNullException.ThrowIfNull(holidays);

        var factors = new Dictionary<int, decimal>();
        var starts = new Dictionary<int, decimal>();
        var level = 1m;
        int? previousKey = null;
        int firstKey = 0;
        DateOnly last = default;

        foreach (var (month, ratePercent) in monthlyRates)
        {
            var key = KeyOf(month);

            if (previousKey is null)
            {
                firstKey = key;
            }
            else if (key != previousKey + 1)
            {
                throw MarketDataErrors.InflationSeries(
                    $"month {month:yyyy-MM} does not follow {last:yyyy-MM}; the series must be consecutive");
            }

            if (ratePercent <= -100m)
            {
                throw MarketDataErrors.InflationSeries($"rate of {month:yyyy-MM} must be above -100%, got {ratePercent}");
            }

            var factor = 1m + (ratePercent / 100m);
            starts[key] = level;
            factors[key] = factor;
            level *= factor;
            previousKey = key;
            last = month;
        }

        return previousKey is null
            ? throw MarketDataErrors.InflationSeries("the series has no month")
            : new InflationIndex(holidays, factors, starts, firstKey, previousKey.Value);
    }

    public decimal Level(DateOnly date)
    {
        if (date < FirstDate || date > LastDate)
        {
            throw MarketDataErrors.InflationDate(date, FirstDate, LastDate);
        }

        var key = KeyOf(date);
        if (date < AdjustedFifteenth(key))
        {
            key--;
        }

        if (key > _lastKey)
        {
            key = _lastKey;
        }

        var start = AdjustedFifteenth(key);
        var end = AdjustedFifteenth(key + 1);
        var factor = _monthlyFactor[key];
        var levelAtStart = _levelAtWindowStart[key];

        if (date == end)
        {
            return levelAtStart * factor;
        }

        var elapsed = BusinessDayCalendar.Count(start, date, _holidays).Value;
        if (elapsed == 0)
        {
            return levelAtStart;
        }

        var total = BusinessDayCalendar.Count(start, end, _holidays).Value;

        return levelAtStart * (decimal)Math.Pow((double)factor, (double)elapsed / total);
    }

    public decimal Factor(DateOnly from, DateOnly baseDate) => Level(baseDate) / Level(from);

    public decimal ToPresentValue(decimal amountInOriginalCurrency, DateOnly date, DateOnly baseDate) =>
        BrazilianCurrency.ToReais(amountInOriginalCurrency, date) * Factor(date, baseDate);

    private static int KeyOf(DateOnly date) => (date.Year * 12) + date.Month - 1;

    private DateOnly AdjustedFifteenth(int key)
    {
        var fourteenth = new DateOnly(key / 12, (key % 12) + 1, 14);
        return BusinessDayCalendar.AddBusinessDays(fourteenth, 1, _holidays);
    }
}
