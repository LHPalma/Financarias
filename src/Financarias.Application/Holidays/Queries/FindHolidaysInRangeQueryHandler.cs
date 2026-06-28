using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Application.Holidays.Queries;

public sealed class FindHolidaysInRangeQueryHandler(
    IApplicationDbContext dbContext
) : IQueryHandler<FindHolidaysInRangeQuery, IReadOnlySet<DateOnly>>
{
    public async Task<IReadOnlySet<DateOnly>> HandleAsync(
        FindHolidaysInRangeQuery query,
        CancellationToken cancellationToken = default)
    {
        var dates = await dbContext.Holidays
            .Where(h => h.CountryCode == query.CountryCode
                        && h.Date >= query.StartDate
                        && h.Date <= query.EndDate)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        return dates.ToHashSet();
    }
}