using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using Financarias.Application.MarketData.Stocks.Import;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.B3.Cotahist.Providers;

internal sealed class CotahistFile(ZipArchive archive, ZipArchiveEntry entry, ILogger logger) : IDailyQuoteFile
{
    private const string QuoteRecord = "01";
    private const string TrailerRecord = "99";

    private int _rowsSkipped;

    public int RowsSkipped => _rowsSkipped;

    public async IAsyncEnumerable<DailyQuoteImportItem> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _rowsSkipped = 0;
        var quoteRecords = 0L;
        long? declaredRecords = null;

        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.Latin1, detectEncodingFromByteOrderMarks: false);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith(QuoteRecord, StringComparison.Ordinal))
            {
                quoteRecords++;
            }
            else if (line.StartsWith(TrailerRecord, StringComparison.Ordinal))
            {
                declaredRecords = ReadDeclaredRecords(line);
            }

            switch (CotahistLineParser.TryParse(line, out var item))
            {
                case CotahistLineResult.Parsed:
                    yield return item!;
                    break;
                case CotahistLineResult.Skipped:
                    _rowsSkipped++;
                    break;
            }
        }

        CheckTrailer(declaredRecords, quoteRecords);
    }

    public ValueTask DisposeAsync()
    {
        archive.Dispose();
        return ValueTask.CompletedTask;
    }

    private static long? ReadDeclaredRecords(string trailer) =>
        trailer.Length >= 42
        && long.TryParse(trailer.AsSpan(31, 11), NumberStyles.None, CultureInfo.InvariantCulture, out var total)
            ? total
            : null;

    private void CheckTrailer(long? declaredRecords, long quoteRecords)
    {
        if (declaredRecords is null)
        {
            logger.LogWarning(
                "COTAHIST {Entry} has no readable trailer; the download may be truncated.",
                entry.FullName);
        }
        else if (declaredRecords != quoteRecords)
        {
            logger.LogWarning(
                "COTAHIST {Entry} trailer declares {Declared} records but {Read} quote records were read.",
                entry.FullName,
                declaredRecords,
                quoteRecords);
        }
    }
}
