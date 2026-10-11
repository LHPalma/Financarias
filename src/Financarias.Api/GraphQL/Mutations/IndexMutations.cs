using Financarias.Application.MarketData.Indexes.DTOs.Requests;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Application.MarketData.Indexes.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class IndexMutations
{
    /// <summary>
    ///     Importa da B3 o fechamento diário de um índice de mercado (hoje, o Ibovespa) em um ano inteiro. Grava por chave natural: o índice e o pregão, então repetir a importação atualiza em vez de duplicar.
    ///
    ///     É uma operação de manutenção e hoje não exige autenticação: qualquer chamador pode dispará-la. Um ano leva alguns segundos.
    ///
    ///     Erro (`extensions.code`): `index.year.invalid`, quando o ano é anterior a 1968. Um ano ainda sem pregões, como um ano futuro, não é erro e devolve zero linhas.
    /// </summary>
    /// <param name="input">Índice e ano a importar.</param>
    [GraphQLName("importIndexQuotes")]
    public Task<IndexQuoteImportResult> ImportIndexQuotesAsync(
        ImportIndexQuotesRequest input,
        IImportIndexQuotesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
