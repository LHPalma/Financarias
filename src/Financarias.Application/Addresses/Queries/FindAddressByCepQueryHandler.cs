using Financarias.Application.Common.Messaging;

namespace Financarias.Application.Addresses.Queries;

public sealed class FindAddressByCepQueryHandler(IAddressLookupProvider provider)
    : IQueryHandler<FindAddressByCepQuery, AddressLookupResult?>
{
    public Task<AddressLookupResult?> HandleAsync(
        FindAddressByCepQuery query,
        CancellationToken cancellationToken = default)
    {
        return provider.FindAddressAsync(query.Cep, cancellationToken);
    }
}
