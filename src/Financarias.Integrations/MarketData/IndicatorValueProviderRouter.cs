using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Bcb.Sgs.Providers;
using Financarias.Integrations.MarketData.Cboe.Providers;
using Financarias.Integrations.MarketData.Eia.Providers;
using Financarias.Integrations.MarketData.Fed.Providers;
using Financarias.Integrations.MarketData.WorldBank.Providers;

namespace Financarias.Integrations.MarketData;

public class IndicatorValueProviderRouter(
    BcbIndicatorValueProvider bcb,
    EiaIndicatorValueProvider eia,
    CboeIndicatorValueProvider cboe,
    FedDollarIndexValueProvider fed,
    WorldBankIndicatorValueProvider worldBank
) : IIndicatorValueProvider
{
    public Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default) =>
        ProviderFor(indicator).GetYearAsync(indicator, year, cancellationToken);

    private IIndicatorValueProvider ProviderFor(EconomicIndicator indicator) => indicator switch
    {
        EconomicIndicator.Dollar or EconomicIndicator.SelicTarget or EconomicIndicator.Cdi or EconomicIndicator.Ipca => bcb,
        EconomicIndicator.BrentOil or EconomicIndicator.WtiOil => eia,
        EconomicIndicator.Vix => cboe,
        EconomicIndicator.DollarIndex => fed,
        EconomicIndicator.IronOre => worldBank,
        _ => throw new ArgumentOutOfRangeException(nameof(indicator), indicator, "No provider for this indicator."),
    };
}
