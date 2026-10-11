using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.Holidays.Specifications;
using Financarias.Application.MarketData.Indicators.Specifications;
using Financarias.Domain.Holidays.Models;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Domain.MarketData.Inflation;

namespace Financarias.Application.MarketData.Inflation.Commands;

public class RebuildInflationLevelsCommandHandler(
    IRepository<IndicatorValue> indicatorValues,
    IRepository<Holiday> holidays,
    IRepository<InflationLevel> levels
) : ICommandHandler<RebuildInflationLevelsCommand, InflationLevelsRebuildResult>
{
    private const int StoredDecimals = 16;

    public async Task<InflationLevelsRebuildResult> HandleAsync(
        RebuildInflationLevelsCommand command,
        CancellationToken cancellationToken = default)
    {
        var monthlyRates = (await indicatorValues.ListAsync(
                new IndicatorValuesByIndicatorSpecification(EconomicIndicator.Ipca),
                cancellationToken))
            .Select(value => (value.ReferenceDate, value.Value));
        var nonBusinessDays = (await holidays.ListAsync(
                new HolidaysByCountrySpecification(CountryCode.BR),
                cancellationToken))
            .Select(holiday => holiday.Date)
            .ToHashSet();

        var index = InflationIndex.FromMonthlyRates(monthlyRates, nonBusinessDays);
        var computed = ComputeDays(index);
        var existing = (await levels.ListAsync(cancellationToken)).ToDictionary(level => level.Day);

        var toCreate = new List<InflationLevel>();
        var updated = 0;

        foreach (var (day, level) in computed)
        {
            if (!existing.TryGetValue(day, out var current))
            {
                toCreate.Add(InflationLevel.Create(day, level));
            }
            else if (current.Level != level)
            {
                current.UpdateLevel(level);
                updated++;
            }
        }

        var computedDays = computed.Select(item => item.Day).ToHashSet();
        var toRemove = existing.Values.Where(level => !computedDays.Contains(level.Day)).ToList();

        if (toCreate.Count > 0)
        {
            await levels.AddRangeAsync(toCreate, cancellationToken);
        }
        else if (updated > 0)
        {
            await levels.SaveChangesAsync(cancellationToken);
        }

        if (toRemove.Count > 0)
        {
            await levels.DeleteRangeAsync(toRemove, cancellationToken);
        }

        return new InflationLevelsRebuildResult(
            index.FirstDate,
            index.LastDate,
            computed.Count,
            toCreate.Count,
            updated,
            toRemove.Count);
    }

    private static List<(DateOnly Day, decimal Level)> ComputeDays(InflationIndex index)
    {
        var days = new List<(DateOnly Day, decimal Level)>(index.LastDate.DayNumber - index.FirstDate.DayNumber + 1);

        for (var day = index.FirstDate; day <= index.LastDate; day = day.AddDays(1))
        {
            days.Add((day, Math.Round(index.Level(day), StoredDecimals, MidpointRounding.AwayFromZero)));
        }

        return days;
    }
}
