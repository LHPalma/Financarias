using System.Globalization;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Fred.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Financarias.Integrations.MarketData.Fred.Providers;

public class FredIndicatorValueProvider(
    IFredClient client,
    IOptions<FredOptions> options,
    ILogger<FredIndicatorValueProvider> logger
) : IIndicatorValueProvider
{
    private const int StoredDecimals = 6;

    public async Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default)
    {
        var apiKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Integrations:Fred:ApiKey not configured. Request a free key at fred.stlouisfed.org and set it with user-secrets or the Integrations__Fred__ApiKey environment variable.");
        }

        var response = await client.GetObservationsAsync(SeriesFor(indicator), $"{year}-01-01", $"{year}-12-31", apiKey, cancellationToken);

        var items = new List<IndicatorValueImportItem>();

        foreach (var observation in response.Observations ?? [])
        {
            if (observation.Value == ".")
            {
                continue;
            }

            if (!DateOnly.TryParseExact(observation.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var referenceDate)
                || !decimal.TryParse(observation.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                logger.LogWarning(
                    "FRED series for {Indicator} has an unusable observation '{Date}' = '{Value}'.",
                    indicator,
                    observation.Date,
                    observation.Value);
                continue;
            }

            items.Add(new IndicatorValueImportItem(indicator, referenceDate, Math.Round(value, StoredDecimals, MidpointRounding.AwayFromZero)));
        }

        return items.OrderBy(item => item.ReferenceDate).ToList();
    }

    private static string SeriesFor(EconomicIndicator indicator) => indicator switch
    {
        EconomicIndicator.BrentOil => "DCOILBRENTEU",
        EconomicIndicator.WtiOil => "DCOILWTICO",
        EconomicIndicator.IronOre => "PIORECRUSDM",
        EconomicIndicator.DollarIndex => "DTWEXBGS",
        EconomicIndicator.Vix => "VIXCLS",
        _ => throw new ArgumentOutOfRangeException(nameof(indicator), indicator, "Not a FRED indicator."),
    };
}
