using Financarias.Application.Addresses;
using Financarias.Application.Addresses.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class AddressQueries
{
    [GraphQLName("addressLookup")]
    public Task<AddressLookupResult?> AddressLookupAsync(
        string cep,
        IFindAddressByCepUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cep, cancellationToken);
}
