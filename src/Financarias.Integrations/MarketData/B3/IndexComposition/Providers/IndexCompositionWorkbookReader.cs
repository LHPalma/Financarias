using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Financarias.Application.MarketData.IndexComposition.Import;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.B3.IndexComposition.Providers;

public sealed partial class IndexCompositionWorkbookReader(ILogger logger)
{
    [GeneratedRegex("^[A-Z0-9]{4,8}$")]
    private static partial Regex TickerPattern();

    [GeneratedRegex(@"(?<d>\d{2})/(?<m>\d{2})/(?<y>\d{4})")]
    private static partial Regex BaseDatePattern();

    public IReadOnlyList<IndexPortfolioImportItem> Read(Stream file)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var portfolios = new List<IndexPortfolioImportItem>();

        foreach (var workbook in Workbooks(file))
        {
            using (workbook)
            {
                portfolios.AddRange(ReadWorkbook(workbook));
            }
        }

        return portfolios;
    }

    private static IEnumerable<Stream> Workbooks(Stream file)
    {
        var header = new byte[2];
        var read = file.Read(header, 0, 2);
        file.Position = 0;

        if (read < 2 || header[0] != 'P' || header[1] != 'K')
        {
            yield return Copy(file);
            yield break;
        }

        using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: true);

        if (archive.GetEntry("xl/workbook.xml") is not null)
        {
            file.Position = 0;
            yield return Copy(file);
            yield break;
        }

        foreach (var entry in archive.Entries)
        {
            if (entry.Length == 0 || IsMacMetadata(entry) || !IsWorkbookName(entry.Name))
            {
                continue;
            }

            using var entryStream = entry.Open();
            yield return Copy(entryStream);
        }
    }

    private static bool IsMacMetadata(ZipArchiveEntry entry) =>
        entry.FullName.StartsWith("__MACOSX/", StringComparison.Ordinal) || entry.Name.StartsWith("._", StringComparison.Ordinal);

    private static bool IsWorkbookName(string name) =>
        name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".xls", StringComparison.OrdinalIgnoreCase);

    private static MemoryStream Copy(Stream source)
    {
        var copy = new MemoryStream();
        source.CopyTo(copy);
        copy.Position = 0;

        return copy;
    }

    private List<IndexPortfolioImportItem> ReadWorkbook(Stream workbook)
    {
        var portfolios = new List<IndexPortfolioImportItem>();

        using var reader = ExcelReaderFactory.CreateReader(workbook);

        do
        {
            var rows = ReadRows(reader);
            if (rows.Count == 0)
            {
                continue;
            }

            if (TryReadSheet(reader.Name, rows) is { } portfolio)
            {
                portfolios.Add(portfolio);
            }
        }
        while (reader.NextResult());

        return portfolios;
    }

    private static List<object?[]> ReadRows(IExcelDataReader reader)
    {
        var rows = new List<object?[]>();

        while (reader.Read())
        {
            var values = new object?[reader.FieldCount];
            for (var column = 0; column < values.Length; column++)
            {
                values[column] = reader.GetValue(column);
            }

            rows.Add(values);
        }

        return rows;
    }

    private IndexPortfolioImportItem? TryReadSheet(string sheetName, List<object?[]> rows)
    {
        var title = IndexCompositionSheetTitle.TryParse(TextOf(rows[0], 0));
        if (title is null)
        {
            return null;
        }

        var headerIndex = rows.FindIndex(row => FindColumns(row) is not null);
        if (headerIndex < 0)
        {
            logger.LogWarning("Sheet {Sheet} has a portfolio title but no recognizable header; skipped.", sheetName);
            return null;
        }

        var columns = FindColumns(rows[headerIndex])!.Value;

        var items = new List<IndexCompositionRowImportItem>();
        decimal? divisor = null;
        DateOnly? baseDate = null;

        foreach (var row in rows.Skip(headerIndex + 1))
        {
            var label = TextOf(row, columns.Code);

            if (label.StartsWith("VALOR DO REDUTOR", StringComparison.OrdinalIgnoreCase))
            {
                divisor ??= ParseDecimal(ValueOf(row, columns.Quantity));
                continue;
            }

            if (label.StartsWith("PREG", StringComparison.OrdinalIgnoreCase) && BaseDatePattern().Match(label) is { Success: true } date)
            {
                baseDate ??= TryDate(date.Groups["y"].Value, date.Groups["m"].Value, date.Groups["d"].Value);
                continue;
            }

            var ticker = label.Trim().ToUpperInvariant();
            if (!TickerPattern().IsMatch(ticker)
                || ParseDecimal(ValueOf(row, columns.Quantity)) is not { } quantity || quantity <= 0
                || ParseDecimal(ValueOf(row, columns.Weight)) is not { } weight)
            {
                continue;
            }

            items.Add(new IndexCompositionRowImportItem(
                ticker, TextOf(row, columns.Name), TextOf(row, columns.Type), (long)Math.Round(quantity), weight));
        }

        if (items.Count == 0)
        {
            return null;
        }

        return new IndexPortfolioImportItem(sheetName.Trim().ToUpperInvariant(), title.Year, title.Quadrimester, title.Kind, baseDate, divisor, items);
    }

    private static DateOnly? TryDate(string year, string month, string day) =>
        DateOnly.TryParseExact($"{year}-{month}-{day}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static Columns? FindColumns(object?[] row)
    {
        int code = -1, name = -1, type = -1, quantity = -1, weight = -1;

        for (var column = 0; column < row.Length; column++)
        {
            var text = IndexCompositionSheetTitle.RemoveDiacritics(TextOf(row, column)).Trim().ToUpperInvariant();

            if (text.StartsWith("COD", StringComparison.Ordinal))
            {
                code = column;
            }
            else if (text is "ACAO" or "EMPRESA" or "NOME")
            {
                name = column;
            }
            else if (text == "TIPO")
            {
                type = column;
            }
            else if (text.StartsWith("QTDE", StringComparison.Ordinal) || text.StartsWith("QUANT", StringComparison.Ordinal))
            {
                quantity = column;
            }
            else if (text.StartsWith("PART", StringComparison.Ordinal))
            {
                weight = column;
            }
        }

        return code >= 0 && quantity >= 0 && weight >= 0 ? new Columns(code, name, type, quantity, weight) : null;
    }

    private static object? ValueOf(object?[] row, int column) => column >= 0 && column < row.Length ? row[column] : null;

    private static string TextOf(object?[] row, int column) =>
        column >= 0 && column < row.Length && row[column] is { } value
            ? Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;

    private static decimal? ParseDecimal(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case double number:
                return (decimal)number;
            case int integer:
                return integer;
            case decimal number:
                return number;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text.Contains(',') && text.Contains('.'))
        {
            text = text.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        }
        else if (text.Contains(','))
        {
            text = text.Replace(',', '.');
        }
        else if (text.Count(character => character == '.') > 1)
        {
            text = text.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private readonly record struct Columns(int Code, int Name, int Type, int Quantity, int Weight);
}
