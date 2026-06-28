using Financarias.Domain.Analytics;

namespace Financarias.Domain.MarketData.Calendar;

public static class BusinessDayCalendar
{
    public static BusinessDayCount Count(DateOnly start, DateOnly end, IReadOnlySet<DateOnly> holidays)
    {
        if (start >= end)
        {
            return BusinessDayCount.Create(0);
        }

        var count = 0;
        for (var date = start.AddDays(1); date <= end; date = date.AddDays(1))
        {
            if (IsBusinessDay(date, holidays))
            {
                count++;
            }
        }

        return BusinessDayCount.Create(count);
    }

    private static bool IsBusinessDay(DateOnly date, IReadOnlySet<DateOnly> holidays)
    {
        return date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(date);
    }

    public static DateOnly AddBusinessDays(DateOnly from, int businessDays, IReadOnlySet<DateOnly> holidays)
    {
        var date = from;
        var added = 0;

        while (added < businessDays)
        {
            date = date.AddDays(1);
            if (IsBusinessDay(date, holidays))
            {
                added++;
            }
        }

        return date;
    }
}