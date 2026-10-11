using System.Globalization;
using System.IO.Compression;
using System.Net;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Domain.MarketData;
using Financarias.Integrations.MarketData.B3.Cotahist.Clients;
using Microsoft.Extensions.Logging;
using Refit;

namespace Financarias.Integrations.MarketData.B3.Cotahist.Providers;

public class B3CotahistProvider(
    IB3CotahistClient client,
    ILogger<B3CotahistProvider> logger
) : IDailyQuoteProvider
{
    public async Task<IDailyQuoteFile?> OpenAsync(QuotePeriod period, CancellationToken cancellationToken = default)
    {
        var fileName = FileNameFor(period);

        await using var download = await TryGetFileAsync(fileName, cancellationToken);
        if (download is null)
        {
            return null;
        }

        var temporaryFile = await CopyToTemporaryFileAsync(download, cancellationToken);

        ZipArchive archive;
        try
        {
            archive = new ZipArchive(temporaryFile, ZipArchiveMode.Read);
        }
        catch
        {
            await temporaryFile.DisposeAsync();
            throw;
        }

        var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".TXT", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            archive.Dispose();
            throw new InvalidDataException($"No .TXT entry found in {fileName}.");
        }

        return new CotahistFile(archive, entry, logger);
    }

    private static string FileNameFor(QuotePeriod period) => period switch
    {
        QuotePeriod.Daily daily => $"COTAHIST_D{daily.Date.ToString("ddMMyyyy", CultureInfo.InvariantCulture)}.ZIP",
        QuotePeriod.Monthly monthly => $"COTAHIST_M{monthly.Month.ToString("00", CultureInfo.InvariantCulture)}{monthly.Year}.ZIP",
        QuotePeriod.Yearly yearly => $"COTAHIST_A{yearly.Year}.ZIP",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown quote period."),
    };

    private static async Task<FileStream> CopyToTemporaryFileAsync(Stream source, CancellationToken cancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cotahist-{Guid.NewGuid():N}.zip");
        var file = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        try
        {
            await source.CopyToAsync(file, cancellationToken);
            file.Position = 0;
            return file;
        }
        catch
        {
            await file.DisposeAsync();
            throw;
        }
    }

    private async Task<Stream?> TryGetFileAsync(string fileName, CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetFileAsync(fileName, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("COTAHIST file {FileName} not published (404).", fileName);
            return null;
        }
    }
}
