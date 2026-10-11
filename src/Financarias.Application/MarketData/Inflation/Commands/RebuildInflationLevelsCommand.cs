using Financarias.Application.Common.Messaging;

namespace Financarias.Application.MarketData.Inflation.Commands;

public sealed record RebuildInflationLevelsCommand : ICommand<InflationLevelsRebuildResult>;
