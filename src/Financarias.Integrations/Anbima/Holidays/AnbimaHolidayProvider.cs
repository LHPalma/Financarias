using System.Globalization;
using System.Text;
using ExcelDataReader;
using Financarias.Application.Holidays.Import;
using Financarias.Domain.Holidays.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Financarias.Integrations.Anbima.Holidays;

public class AnbimaHolidayProvider(
    IOptions<AnbimaOptions> options,
    ILogger<AnbimaHolidayProvider> logger
) : IHolidayProvider
{
    // Layout da planilha da Anbima: Data | Dia da Semana | Feriado
    private const int DateColumn = 0;
    private const int NameColumn = 2;

    static AnbimaHolidayProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Task<IReadOnlyList<HolidayImportItem>> FetchHolidaysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var stream = File.OpenRead(options.Value.HolidaysFilePath);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var holidays = new List<HolidayImportItem>();

            while (reader.Read())
            {
                if (reader.FieldCount <= NameColumn)
                {
                    continue;
                }

                var date = TryReadDate(reader.GetValue(DateColumn));
                if (date is null)
                {
                    continue;
                }

                var name = reader.GetValue(NameColumn)?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                holidays.Add(new HolidayImportItem(date.Value, name, CountryCode.BR));
            }

            return Task.FromResult<IReadOnlyList<HolidayImportItem>>(holidays);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch holidays from Anbima file.");
            return Task.FromResult<IReadOnlyList<HolidayImportItem>>([]);
        }
    }

    private static DateOnly? TryReadDate(object? cell) => cell switch
    {
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        double serial     => DateOnly.FromDateTime(DateTime.FromOADate(serial)),
        string text       => TryParseDate(text),
        _                 => null,
    };

    private static DateOnly? TryParseDate(string text)
    {
        string[] formats = ["M/d/yyyy", "dd/MM/yyyy", "yyyy-MM-dd"];

        return DateOnly.TryParseExact(
            text.Trim(),
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }
}