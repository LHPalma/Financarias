using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Domain.Addresses;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Integrations.MarketData.Anp.Fuel.Clients;
using Microsoft.Extensions.Logging;
using Refit;

namespace Financarias.Integrations.MarketData.Anp.Fuel.Providers;

public class AnpFuelProvider(
    IAnpFuelClient client,
    ILogger<AnpFuelProvider> logger
) : IFuelPriceProvider
{
    // Layout do CSV "ca-AAAA-SS" da ANP (posição das colunas).
    private const int RegionColumn = 0;
    private const int StateColumn = 1;
    private const int MunicipalityColumn = 2;
    private const int NameColumn = 3;
    private const int CnpjColumn = 4;
    private const int StreetColumn = 5;
    private const int NumberColumn = 6;
    private const int ComplementColumn = 7;
    private const int NeighborhoodColumn = 8;
    private const int CepColumn = 9;
    private const int ProductColumn = 10;
    private const int CollectedOnColumn = 11;
    private const int SalePriceColumn = 12;
    private const int PurchasePriceColumn = 13;
    private const int MeasureUnitColumn = 14;
    private const int BrandColumn = 15;

    private static readonly CultureInfo PtBr = new("pt-BR");

    private static readonly CsvConfiguration CsvConfig = new(PtBr)
    {
        Delimiter = ";",
        HasHeaderRecord = true,
        BadDataFound = null,
        MissingFieldFound = null,
    };

    public async IAsyncEnumerable<FuelPriceImportItem> FetchFuelPricesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var buffer = await DownloadLatestAvailableAsync(cancellationToken);
        if (buffer is null)
        {
            yield break;
        }

        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read);
        var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            logger.LogWarning("No CSV entry found in ANP file.");
            yield break;
        }

        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8);
        using var csv = new CsvReader(reader, CsvConfig);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            if (TryMapRow(csv, out var item))
            {
                yield return item;
            }
        }
    }

    private bool TryMapRow(IReaderRow row, out FuelPriceImportItem item)
    {
        item = null!;
        try
        {
            if (!Cnpj.TryCreate(row.GetField(CnpjColumn), out var cnpj))
            {
                return false;
            }

            var name = row.GetField(NameColumn)?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (MapRegion(row.GetField(RegionColumn)) is not { } region)
            {
                return false;
            }

            if (MapProduct(row.GetField(ProductColumn)) is not { } product)
            {
                return false;
            }

            if (!TryParseDate(row.GetField(CollectedOnColumn), out var collectedOn))
            {
                return false;
            }

            if (!TryParseDecimal(row.GetField(SalePriceColumn), out var salePrice) || salePrice <= 0)
            {
                return false;
            }

            Cep.TryCreate(row.GetField(CepColumn), out var postalCode);
            var purchasePrice = TryParseDecimal(row.GetField(PurchasePriceColumn), out var purchase) ? purchase : (decimal?)null;

            item = new FuelPriceImportItem(
                cnpj,
                name,
                NormalizeBrand(row.GetField(BrandColumn)),
                region,
                row.GetField(StateColumn)?.Trim() ?? string.Empty,
                row.GetField(MunicipalityColumn)?.Trim() ?? string.Empty,
                Nullify(row.GetField(StreetColumn)),
                Nullify(row.GetField(NumberColumn)),
                Nullify(row.GetField(ComplementColumn)),
                Nullify(row.GetField(NeighborhoodColumn)),
                postalCode,
                product,
                collectedOn,
                salePrice,
                purchasePrice,
                row.GetField(MeasureUnitColumn)?.Trim() ?? string.Empty);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Skipping malformed ANP row.");
            return false;
        }
    }

    private async Task<MemoryStream?> DownloadLatestAvailableAsync(CancellationToken cancellationToken)
    {
        foreach (var fileId in CandidateFileIds())
        {
            var download = await TryGetFileAsync(fileId, cancellationToken);
            if (download is null)
            {
                continue;
            }

            await using (download)
            {
                var buffer = new MemoryStream();
                await download.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;
                return buffer;
            }
        }

        logger.LogWarning("No ANP fuel price file available for the current or previous semester.");
        return null;
    }

    private async Task<Stream?> TryGetFileAsync(string fileId, CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetFuelPricesFileAsync(fileId, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("ANP file ca-{FileId}.zip not published yet (404); trying previous semester.", fileId);
            return null;
        }
    }

    private static IEnumerable<string> CandidateFileIds()
    {
        var now = DateTime.UtcNow;
        var currentSemester = now.Month <= 6 ? 1 : 2;

        yield return SemesterFileId(now.Year, currentSemester);
        yield return currentSemester == 1
            ? SemesterFileId(now.Year - 1, 2)
            : SemesterFileId(now.Year, 1);
    }

    private static string SemesterFileId(int year, int semester) => $"{year}-{semester:00}";

    private static Region? MapRegion(string? sigla) => sigla?.Trim().ToUpperInvariant() switch
    {
        "N" => Region.North,
        "NE" => Region.Northeast,
        "CO" => Region.CenterWest,
        "SE" => Region.Southeast,
        "S" => Region.South,
        _ => null,
    };

    private static FuelProduct? MapProduct(string? produto) => produto?.Trim().ToUpperInvariant() switch
    {
        "GASOLINA" => FuelProduct.Gasoline,
        "GASOLINA ADITIVADA" => FuelProduct.PremiumGasoline,
        "ETANOL" => FuelProduct.Ethanol,
        "DIESEL" => FuelProduct.Diesel,
        "DIESEL S10" => FuelProduct.DieselS10,
        "GNV" => FuelProduct.Cng,
        _ => null,
    };

    private static bool TryParseDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value?.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool TryParseDecimal(string? value, out decimal result) =>
        decimal.TryParse(value?.Trim(), NumberStyles.Number, PtBr, out result);

    private static string NormalizeBrand(string? brand) => brand?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? Nullify(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
