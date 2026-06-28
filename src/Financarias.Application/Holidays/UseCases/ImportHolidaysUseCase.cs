using Financarias.Application.Common.Persistence;
using Financarias.Application.Holidays.Import;
using Financarias.Domain.Holidays.Models;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Application.Holidays.UseCases;

public class ImportHolidaysUseCase(
    IHolidayProvider provider,
    IApplicationDbContext dbContext,
    IRepository<Holiday> repository
) : IImportHolidaysUseCase
{
    public async Task<HolidayImportResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var fetched = await provider.FetchHolidaysAsync(cancellationToken);
        if (fetched.Count == 0)
        {
            return new HolidayImportResult(0, 0);
        }

        var existing = await dbContext.Holidays
            .Select(h => new { h.Date, h.CountryCode })
            .ToListAsync(cancellationToken);

        var existingKeys = existing.Select(x => (x.Date, x.CountryCode)).ToHashSet();

        var toSave = fetched
            .Where(item => !existingKeys.Contains((item.Date, item.CountryCode)))
            .DistinctBy(item => (item.Date, item.CountryCode))
            .Select(item => Holiday.Create(item.Date, item.Name, item.CountryCode))
            .ToList();

        if (toSave.Count > 0)
        {
            await repository.AddRangeAsync(toSave, cancellationToken);
        }

        return new HolidayImportResult(fetched.Count, toSave.Count);
    }
}