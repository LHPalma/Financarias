using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.Import;

public sealed record IndicatorValueImportItem(
    EconomicIndicator Indicator,
    DateOnly ReferenceDate,
    decimal Value);
