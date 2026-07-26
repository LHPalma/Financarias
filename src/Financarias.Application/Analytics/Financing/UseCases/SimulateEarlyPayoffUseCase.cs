using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Application.Analytics.Financing.Queries;
using Financarias.Application.Common.Messaging;

namespace Financarias.Application.Analytics.Financing.UseCases;

public sealed class SimulateEarlyPayoffUseCase(
    SimulateEarlyPayoffMapper mapper,
    IQueryHandler<SimulateEarlyPayoffQuery, EarlyPayoffResult> handler
) : ISimulateEarlyPayoffUseCase
{
    public Task<EarlyPayoffResult> ExecuteAsync(
        SimulateEarlyPayoffRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = mapper.ToQuery(request);
        return handler.HandleAsync(query, cancellationToken);
    }
}
