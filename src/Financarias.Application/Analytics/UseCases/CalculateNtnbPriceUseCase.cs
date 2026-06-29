using Financarias.Application.Analytics.DTOs.Requests;
using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Analytics.Mappers;
using Financarias.Application.Analytics.Queries;
using Financarias.Application.Common.Messaging;

namespace Financarias.Application.Analytics.UseCases;

public sealed class CalculateNtnbPriceUseCase(
    CalculateNtnbPriceMapper mapper,
    IQueryHandler<CalculateNtnbPriceQuery, NtnbPriceResult> handler
) : ICalculateNtnbPriceUseCase
{
    public Task<NtnbPriceResult> ExecuteAsync(
        CalculateNtnbPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = mapper.ToQuery(request);
        return handler.HandleAsync(query, cancellationToken);
    }
}
