using Financarias.Application.MarketData.IndexComposition.DTOs.Requests;
using Financarias.Application.MarketData.IndexComposition.Import;
using Financarias.Application.MarketData.IndexComposition.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class IndexCompositionMutations
{
    /// <summary>
    ///     Importa as carteiras teóricas históricas dos índices da B3 (Ibovespa, IBrX, SMLL, IDIV e os demais) a partir de arquivos publicados pela própria B3: a quantidade teórica de cada ação, o peso divulgado, o pregão-base e o redutor, de cada prévia e de cada virada de quadrimestre. Com quantidade e preço, o peso de uma ação em qualquer dia se recalcula exatamente.
    ///
    ///     Aceita planilhas e ZIPs com várias planilhas; uma planilha de entradas e saídas, que não traz carteira, é lida sem erro e conta zero carteiras. Grava por chave natural (índice, quadrimestre, tipo de carteira e ação), então repetir a importação atualiza em vez de duplicar. Um arquivo que falha entra em `failures` e não impede os demais.
    ///
    ///     É uma operação de manutenção e hoje não exige autenticação: qualquer chamador pode dispará-la.
    ///
    ///     Erro (`extensions.code`): `indexcomposition.source.invalid`, quando algum endereço não é `https://www.b3.com.br/data/files/...`.
    /// </summary>
    /// <param name="input">Endereços dos arquivos a importar.</param>
    [GraphQLName("importIndexComposition")]
    public Task<IndexCompositionImportResult> ImportIndexCompositionAsync(
        ImportIndexCompositionRequest input,
        IImportIndexCompositionUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
