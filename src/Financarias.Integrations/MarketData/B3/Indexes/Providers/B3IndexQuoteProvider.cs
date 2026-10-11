using System.Globalization;
using System.Text;
using System.Text.Json;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Domain.MarketData.Indexes;
using Financarias.Integrations.MarketData.B3.Indexes.Clients;
using Financarias.Integrations.MarketData.B3.Indexes.DTOs.Responses;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.B3.Indexes.Providers;

public class B3IndexQuoteProvider(
    IB3IndexClient client,
    ILogger<B3IndexQuoteProvider> logger
) : IIndexQuoteProvider
{
    public async Task<IReadOnlyList<IndexQuoteImportItem>> GetYearAsync(
        MarketIndex marketIndex,
        int year,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetYearAsync(PayloadFor(marketIndex, year), cancellationToken);

        var items = new List<IndexQuoteImportItem>();

        foreach (var row in response.Results ?? [])
        {
            var values = MonthlyValues(row);

            for (var month = 1; month <= 12; month++)
            {
                var text = values[month - 1];
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (!TryBuildItem(marketIndex, year, month, row.Day, text, out var item))
                {
                    logger.LogWarning(
                        "B3 index {Index} {Year}-{Month:00}-{Day:00} has an unusable value '{Value}'.",
                        marketIndex,
                        year,
                        month,
                        row.Day,
                        text);
                    continue;
                }

                items.Add(item);
            }
        }

        return items.OrderBy(item => item.TradingDate).ToList();
    }

    private static string PayloadFor(MarketIndex marketIndex, int year)
    {
        var json = JsonSerializer.Serialize(new
        {
            language = "pt-br",
            index = CodeFor(marketIndex),
            year = year.ToString(CultureInfo.InvariantCulture),
        });

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static string CodeFor(MarketIndex marketIndex) => marketIndex switch
    {
        MarketIndex.Ibovespa => "IBOV",
        _ => throw new ArgumentOutOfRangeException(nameof(marketIndex), marketIndex, "Unknown market index."),
    };

    private static string?[] MonthlyValues(B3IndexStatisticsRow row) =>
    [
        row.RateValue1,
        row.RateValue2,
        row.RateValue3,
        row.RateValue4,
        row.RateValue5,
        row.RateValue6,
        row.RateValue7,
        row.RateValue8,
        row.RateValue9,
        row.RateValue10,
        row.RateValue11,
        row.RateValue12,
    ];

    private static bool TryBuildItem(
        MarketIndex marketIndex,
        int year,
        int month,
        int day,
        string text,
        out IndexQuoteImportItem item)
    {
        item = null!;

        if (day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        var normalized = text.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var closeValue)
            || closeValue <= 0)
        {
            return false;
        }

        item = new IndexQuoteImportItem(marketIndex, new DateOnly(year, month, day), closeValue, IndexQuoteSource.B3);
        return true;
    }
}
