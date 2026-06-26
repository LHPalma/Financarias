using Financarias.Application.Addresses;
using Financarias.Application.Addresses.UseCases;

namespace Financarias.Api.GraphQL;

public class Query
{
    [GraphQLName("addressLookup")]
    public Task<AddressLookupResult?> AddressLookupAsync(
        string cep,
        IFindAddressByCepUseCase useCase,
        CancellationToken cancellationToken)
    {
        return useCase.ExecuteAsync(cep, cancellationToken);
    }
}
