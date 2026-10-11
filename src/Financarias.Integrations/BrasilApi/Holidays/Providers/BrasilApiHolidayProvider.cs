using System.Globalization;
using Financarias.Application.Holidays.Import;
using Financarias.Domain.Holidays.Models;
using Financarias.Integrations.BrasilApi.Holidays.Clients;
using Financarias.Integrations.BrasilApi.Holidays.DTOs.Responses;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.BrasilApi.Holidays.Providers;

public class BrasilApiHolidayProvider(
    IBrasilApiClient client,
    ILogger<BrasilApiHolidayProvider> logger
) : IHolidayProvider
{
    public const int FirstYear = 1980;
    public const int LastYear = 2000;

    private const string NationalType = "national";
    private const string Easter = "Páscoa";

    public async Task<IReadOnlyList<HolidayImportItem>> FetchHolidaysAsync(
        CancellationToken cancellationToken = default)
    {
        var holidays = new List<HolidayImportItem>();

        for (var year = FirstYear; year <= LastYear; year++)
        {
            try
            {
                var answer = await client.GetHolidaysAsync(year, cancellationToken);
                holidays.AddRange(answer.Select(Map).OfType<HolidayImportItem>());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to fetch holidays of {Year} from BrasilAPI.", year);
            }
        }

        return holidays;
    }

    private static HolidayImportItem? Map(BrasilApiHoliday holiday)
    {
        if (!string.Equals(holiday.Type, NationalType, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(holiday.Name)
            || string.Equals(holiday.Name.Trim(), Easter, StringComparison.OrdinalIgnoreCase)
            || !DateOnly.TryParseExact(holiday.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        return new HolidayImportItem(date, holiday.Name.Trim(), CountryCode.BR);
    }
}
