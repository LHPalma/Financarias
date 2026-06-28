using Financarias.Application.Common.Messaging;
using Financarias.Application.Holidays.Queries;
using Financarias.Domain.Calendar;
using Financarias.Domain.Holidays.Models;

namespace Financarias.Application.Holidays.UseCases;

public sealed class CountBusinessDaysUseCase(
    IQueryHandler<FindHolidaysInRangeQuery, IReadOnlySet<DateOnly>> handler
) : ICountBusinessDaysUseCase
{
    public async Task<int> ExecuteAsync(DateOnly start, DateOnly end, CountryCode countryCode,
        CancellationToken cancellationToken = default)
    {
        var holidays = await handler.HandleAsync(
            new FindHolidaysInRangeQuery(start, end, countryCode),
            cancellationToken);

        return BusinessDayCalendar.Count(start, end, holidays).Value;
    }
}