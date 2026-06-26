using Financarias.Application.Addresses.Queries;
using Financarias.Application.Common.Messaging;
using Financarias.Domain.Addresses;

namespace Financarias.Application.Addresses.UseCases;

public sealed class FindAddressByCepUseCase(
    IQueryHandler<FindAddressByCepQuery, AddressLookupResult?> handler
) : IFindAddressByCepUseCase
{
    public async Task<AddressLookupResult?> ExecuteAsync(string cep, CancellationToken cancellationToken = default)
    {
        var validCep = Cep.Create(cep);

        return await handler.HandleAsync(new FindAddressByCepQuery(validCep), cancellationToken);
    }
}
