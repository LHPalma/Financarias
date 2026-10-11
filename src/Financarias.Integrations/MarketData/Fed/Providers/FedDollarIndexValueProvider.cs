using System.Globalization;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Fed.Clients;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.Fed.Providers;

public class FedDollarIndexValueProvider(
    IFedDataDownloadClient client,
    ILogger<FedDollarIndexValueProvider> logger
) : IIndicatorValueProvider
{
    // Identificador da série "Nominal Broad Dollar Index" (JRXWTFB_N.B) no Data Download Program do Fed
    private const string BroadDollarIndexSeries = "122e3bcb627e8e53f1bf72a1a09cfb81";
    private const int StoredDecimals = 6;

    public async Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (indicator != EconomicIndicator.DollarIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(indicator), indicator, "Not a Federal Reserve indicator.");
        }

        var csv = await client.GetSeriesAsync(BroadDollarIndexSeries, $"01/01/{year}", $"12/31/{year}", cancellationToken);
        var items = new List<IndicatorValueImportItem>();

        foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var columns = line.Trim().Split(',');

            // Cabeçalho tem aspas; dados são "yyyy-MM-dd,valor,..." e dias sem dado vêm como ND
            if (columns.Length < 2
                || !DateOnly.TryParseExact(columns[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var referenceDate))
            {
                continue;
            }

            if (columns[1] == "ND")
            {
                continue;
            }

            if (!decimal.TryParse(columns[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                logger.LogWarning("Federal Reserve dollar index has an unusable line '{Line}'.", line.Trim());
                continue;
            }

            items.Add(new IndicatorValueImportItem(indicator, referenceDate, Math.Round(value, StoredDecimals, MidpointRounding.AwayFromZero)));
        }

        return items.OrderBy(item => item.ReferenceDate).ToList();
    }
}
