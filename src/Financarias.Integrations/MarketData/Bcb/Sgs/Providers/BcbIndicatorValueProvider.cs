using System.Globalization;
using System.Net;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Bcb.Sgs.Clients;
using Financarias.Integrations.MarketData.Bcb.Sgs.DTOs.Responses;
using Microsoft.Extensions.Logging;
using Refit;

namespace Financarias.Integrations.MarketData.Bcb.Sgs.Providers;

public class BcbIndicatorValueProvider(
    IBcbSgsClient client,
    TimeProvider timeProvider,
    ILogger<BcbIndicatorValueProvider> logger
) : IIndicatorValueProvider
{
    public async Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default)
    {
        var points = await FetchAsync(indicator, year, cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var items = new List<IndicatorValueImportItem>();

        foreach (var point in points)
        {
            if (!TryParse(point.Data, point.Valor, out var referenceDate, out var value))
            {
                logger.LogWarning(
                    "BCB series for {Indicator} has an unusable point '{Date}' = '{Value}'.",
                    indicator,
                    point.Data,
                    point.Valor);
                continue;
            }

            if (referenceDate > today)
            {
                continue;
            }

            items.Add(new IndicatorValueImportItem(indicator, referenceDate, value));
        }

        return items.OrderBy(item => item.ReferenceDate).ToList();
    }

    private static int SeriesFor(EconomicIndicator indicator) => indicator switch
    {
        EconomicIndicator.Dollar => 1,
        EconomicIndicator.SelicTarget => 432,
        EconomicIndicator.Cdi => 4389,
        EconomicIndicator.Ipca => 433,
        _ => throw new ArgumentOutOfRangeException(nameof(indicator), indicator, "Unknown economic indicator."),
    };

    private static bool TryParse(string? date, string? text, out DateOnly referenceDate, out decimal value)
    {
        value = 0;

        return DateOnly.TryParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out referenceDate)
               && decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    private async Task<IReadOnlyList<BcbSgsPoint>> FetchAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetSeriesAsync(SeriesFor(indicator), $"01/01/{year}", $"31/12/{year}", cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("BCB series for {Indicator} has no data in {Year} (404).", indicator, year);
            return [];
        }
    }
}
