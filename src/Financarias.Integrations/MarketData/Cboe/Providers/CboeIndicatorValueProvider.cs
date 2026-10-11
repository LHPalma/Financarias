using System.Globalization;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Cboe.Clients;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.Cboe.Providers;

public class CboeIndicatorValueProvider(
    ICboeClient client,
    ILogger<CboeIndicatorValueProvider> logger
) : IIndicatorValueProvider
{
    // CSV da Cboe: DATE,OPEN,HIGH,LOW,CLOSE com data MM/dd/yyyy
    private const int DateColumn = 0;
    private const int CloseColumn = 4;
    private const int StoredDecimals = 6;

    public async Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (indicator != EconomicIndicator.Vix)
        {
            throw new ArgumentOutOfRangeException(nameof(indicator), indicator, "Not a Cboe indicator.");
        }

        var csv = await client.GetVixHistoryAsync(cancellationToken);
        var items = new List<IndicatorValueImportItem>();

        foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var columns = line.Trim().Split(',');

            if (columns.Length <= CloseColumn)
            {
                continue;
            }

            if (!DateOnly.TryParseExact(columns[DateColumn], "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var referenceDate)
                || !decimal.TryParse(columns[CloseColumn], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                logger.LogWarning("Cboe VIX history has an unusable line '{Line}'.", line.Trim());
                continue;
            }

            if (referenceDate.Year == year)
            {
                items.Add(new IndicatorValueImportItem(indicator, referenceDate, Math.Round(value, StoredDecimals, MidpointRounding.AwayFromZero)));
            }
        }

        return items.OrderBy(item => item.ReferenceDate).ToList();
    }
}
