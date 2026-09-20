using Financarias.Application.Addresses;
using Financarias.Application.Addresses.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class AddressQueries
{
    /// <summary>
    ///     Consulta o endereço de um CEP (fonte: ViaCEP). Devolve nulo se o CEP não existir. Não exige autenticação.
    ///
    ///     Erro (`extensions.code`): `address.cep.invalid`, quando o valor não tem 8 dígitos. Máscara e outros caracteres que não sejam dígitos são ignorados.
    /// </summary>
    /// <param name="cep">CEP, com ou sem máscara (01001-000 ou 01001000).</param>
    [GraphQLName("addressLookup")]
    public Task<AddressLookupResult?> AddressLookupAsync(
        string cep,
        IFindAddressByCepUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cep, cancellationToken);
}
