using System.Globalization;
using System.Text;
using ExcelDataReader;
using Financarias.Application.Holidays.Import;
using Financarias.Domain.Holidays.Models;
using Financarias.Integrations.Anbima.Holidays.Clients;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.Anbima.Holidays.Providers;

public class AnbimaHolidayProvider(
    IAnbimaHolidayClient client,
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

    public async Task<IReadOnlyList<HolidayImportItem>> FetchHolidaysAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var download = await client.GetHolidaysFileAsync(cancellationToken);
            using var buffer = new MemoryStream();
            await download.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            using var reader = ExcelReaderFactory.CreateReader(buffer);

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

            return holidays;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch holidays from Anbima file.");
            return [];
        }
    }

    private static DateOnly? TryReadDate(object? cell)
    {
        return cell switch
        {
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            double serial     => DateOnly.FromDateTime(DateTime.FromOADate(serial)),
            string text       => TryParseDate(text),
            _                 => null,
        };
    }

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