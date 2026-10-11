using Financarias.Application.MarketData.Indicators.DTOs.Requests;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Application.MarketData.Indicators.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class IndicatorMutations
{
    /// <summary>
    ///     Importa do Banco Central (séries do SGS) os valores de um indicador econômico, como o dólar, a meta Selic, o CDI ou o IPCA, em um ano inteiro. Grava por chave natural: o indicador e a data de referência, então repetir a importação atualiza em vez de duplicar.
    ///
    ///     É uma operação de manutenção e hoje não exige autenticação: qualquer chamador pode dispará-la. Um ano leva alguns segundos. Datas futuras que o Banco Central já publica, como a meta Selic de reuniões agendadas, não são gravadas.
    ///
    ///     Erro (`extensions.code`): `indicator.year.invalid`, quando o ano é anterior a 1980. Um ano em que a série ainda não existia não é erro e devolve zero linhas.
    /// </summary>
    /// <param name="input">Indicador e ano a importar.</param>
    [GraphQLName("importIndicatorValues")]
    public Task<IndicatorValueImportResult> ImportIndicatorValuesAsync(
        ImportIndicatorValuesRequest input,
        IImportIndicatorValuesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
