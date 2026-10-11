using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.WorldBank.Clients;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.WorldBank.Providers;

public partial class WorldBankIndicatorValueProvider(
    IWorldBankPageClient pageClient,
    IWorldBankDocsClient docsClient,
    ILogger<WorldBankIndicatorValueProvider> logger
) : IIndicatorValueProvider
{
    // Planilha "Pink Sheet": aba "Monthly Prices", a coluna do minério tem "Iron ore" no cabeçalho
    // e as linhas de dados começam com o período no formato 2026M07
    private const string PricesSheet = "Monthly Prices";
    private const string IronOreHeader = "Iron ore";
    private const int StoredDecimals = 6;

    public async Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (indicator != EconomicIndicator.IronOre)
        {
            throw new ArgumentOutOfRangeException(nameof(indicator), indicator, "Not a World Bank indicator.");
        }

        // O endereço da planilha leva um código que muda a cada edição, então é lido da página
        var page = await pageClient.GetCommodityMarketsPageAsync(cancellationToken);
        var match = ReleaseLink().Match(page);

        if (!match.Success)
        {
            logger.LogWarning("World Bank commodity markets page has no link to the monthly prices workbook.");
            return [];
        }

        await using var download = await docsClient.GetMonthlyPricesFileAsync(match.Groups["release"].Value, cancellationToken);
        using var buffer = new MemoryStream();
        await download.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        using var reader = ExcelReaderFactory.CreateReader(buffer);

        do
        {
            if (reader.Name == PricesSheet)
            {
                return ReadYear(reader, indicator, year);
            }
        }
        while (reader.NextResult());

        logger.LogWarning("World Bank workbook has no '{Sheet}' sheet.", PricesSheet);

        return [];
    }

    [GeneratedRegex(@"/en/doc/(?<release>[^/""'\s]+)/related/CMO-Historical-Data-Monthly\.xlsx")]
    private static partial Regex ReleaseLink();

    private static List<IndicatorValueImportItem> ReadYear(IExcelDataReader reader, EconomicIndicator indicator, int year)
    {
        var items = new List<IndicatorValueImportItem>();
        var column = -1;

        while (reader.Read())
        {
            if (column < 0)
            {
                column = FindIronOreColumn(reader);
                continue;
            }

            if (!TryReadPeriod(reader.GetValue(0)?.ToString(), out var month) || month.Year != year)
            {
                continue;
            }

            if (reader.FieldCount <= column || reader.GetValue(column) is not double price)
            {
                continue;
            }

            var value = Math.Round(Convert.ToDecimal(price, CultureInfo.InvariantCulture), StoredDecimals, MidpointRounding.AwayFromZero);
            items.Add(new IndicatorValueImportItem(indicator, month, value));
        }

        return items.OrderBy(item => item.ReferenceDate).ToList();
    }

    private static int FindIronOreColumn(IExcelDataReader reader)
    {
        for (var i = 1; i < reader.FieldCount; i++)
        {
            var header = reader.GetValue(i)?.ToString();

            if (header is not null && header.Contains(IronOreHeader, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryReadPeriod(string? text, out DateOnly firstOfMonth)
    {
        firstOfMonth = default;

        return text is { Length: 7 }
               && int.TryParse(text.AsSpan(0, 4), CultureInfo.InvariantCulture, out var year)
               && text[4] == 'M'
               && int.TryParse(text.AsSpan(5, 2), CultureInfo.InvariantCulture, out var month)
               && month is >= 1 and <= 12
               && (firstOfMonth = new DateOnly(year, month, 1)) != default;
    }
}
