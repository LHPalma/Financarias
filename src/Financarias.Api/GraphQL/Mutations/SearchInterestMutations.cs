using Financarias.Application.MarketData.SearchInterest.DTOs.Requests;
using Financarias.Application.MarketData.SearchInterest.Import;
using Financarias.Application.MarketData.SearchInterest.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class SearchInterestMutations
{
    /// <summary>
    ///     Importa do Google Trends o interesse de busca diário de um grupo de 1 a 5 termos em um país, num intervalo de até 270 dias, e grava por grupo, intervalo, termo e dia: repetir a importação atualiza em vez de duplicar.
    ///
    ///     O índice de 0 a 100 do Google é **relativo ao grupo e ao intervalo**: 100 é o maior valor entre todos os termos e dias da consulta. Por isso o grupo e o intervalo fazem parte da chave, e só se comparam valores do mesmo grupo e intervalo. O último dia, se for hoje, é parcial e não é gravado.
    ///
    ///     A fonte é a interface pública do Google Trends, sem chave e **sem contrato**: o Google pode mudar o formato ou bloquear o acesso a qualquer momento (a resposta chega como erro HTTP 429 ou JSON inesperado). É uma operação de manutenção e hoje não exige autenticação: qualquer chamador pode dispará-la.
    ///
    ///     Erro (`extensions.code`): `searchinterest.point.invalid`, quando o grupo não tem de 1 a 5 termos, o país não são duas letras maiúsculas (ou país e região, como `BR-SP`) ou o intervalo é invertido ou passa de 270 dias.
    /// </summary>
    /// <param name="input">Termos, país e intervalo.</param>
    [GraphQLName("importSearchInterest")]
    public Task<SearchInterestImportResult> ImportSearchInterestAsync(
        ImportSearchInterestRequest input,
        IImportSearchInterestUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
