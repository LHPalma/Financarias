using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Application.MarketData.Fuel.Queries;

namespace Financarias.Application.MarketData.Fuel.UseCases;

public class FindEthanolGasolineParityUseCase(
    IQueryHandler<FindEthanolGasolineParityQuery, IQueryable<EthanolGasolineParityResult>> handler
) : IFindEthanolGasolineParityUseCase
{
    public Task<IQueryable<EthanolGasolineParityResult>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        handler.HandleAsync(new FindEthanolGasolineParityQuery(), cancellationToken);
}
