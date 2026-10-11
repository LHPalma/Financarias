using Financarias.Application.MarketData.Issuers.Import;
using Financarias.Application.MarketData.Issuers.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class IssuerMutations
{
    /// <summary>
    ///     Cadastra a partir da B3 os emissores que têm cotações já importadas: razão social, nome de pregão, CNPJ, código CVM, mercado de listagem e a classificação setorial (setor, subsetor e segmento). Grava só o que é novo ou mudou, então repetir não duplica.
    ///
    ///     O conjunto tentado sai das cotações, pelo prefixo do ISIN; um emissor que a B3 não lista hoje, como uma empresa que saiu da bolsa, não ganha cadastro e entra na contagem `notListed`.
    ///
    ///     É uma operação de manutenção, com uma chamada à B3 por emissor listado (alguns minutos para a base inteira), e hoje não exige autenticação: qualquer chamador pode dispará-la.
    /// </summary>
    [GraphQLName("importIssuers")]
    public Task<IssuerImportResult> ImportIssuersAsync(
        IImportIssuersUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}
