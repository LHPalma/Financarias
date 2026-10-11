using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Bcb.Sgs.Providers;
using Financarias.Integrations.MarketData.Fred.Providers;

namespace Financarias.Integrations.MarketData;

public class IndicatorValueProviderRouter(
    BcbIndicatorValueProvider bcb,
    FredIndicatorValueProvider fred
) : IIndicatorValueProvider
{
    public Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default) =>
        indicator switch
        {
            EconomicIndicator.Dollar or EconomicIndicator.SelicTarget or EconomicIndicator.Cdi or EconomicIndicator.Ipca =>
                bcb.GetYearAsync(indicator, year, cancellationToken),
            _ => fred.GetYearAsync(indicator, year, cancellationToken),
        };
}
