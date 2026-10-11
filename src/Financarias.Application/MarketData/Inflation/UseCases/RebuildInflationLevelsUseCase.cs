using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Inflation.Commands;

namespace Financarias.Application.MarketData.Inflation.UseCases;

public class RebuildInflationLevelsUseCase(
    ICommandHandler<RebuildInflationLevelsCommand, InflationLevelsRebuildResult> handler
) : IRebuildInflationLevelsUseCase
{
    public Task<InflationLevelsRebuildResult> ExecuteAsync(CancellationToken cancellationToken = default) =>
        handler.HandleAsync(new RebuildInflationLevelsCommand(), cancellationToken);
}
