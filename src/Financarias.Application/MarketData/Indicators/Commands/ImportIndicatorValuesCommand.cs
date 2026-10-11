using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.Commands;

public sealed record ImportIndicatorValuesCommand(EconomicIndicator Indicator, int Year)
    : ICommand<IndicatorValueImportResult>;
