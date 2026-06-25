using Financarias.Application.Addresses;

namespace Financarias.Api.GraphQL;

public class Query
{
    [GraphQLName("addressLookup")]
    public async Task<AddressLookupResult?> AddressLookupAsync(
        string cep,
        IAddressLookupProvider addressLookupProvider,
        CancellationToken cancellationToken)
    {
        return await addressLookupProvider.FindAddressAsync(cep, cancellationToken);
    }
}