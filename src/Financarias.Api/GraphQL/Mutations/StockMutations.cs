using Financarias.Application.MarketData.Stocks.DTOs.Requests;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Application.MarketData.Stocks.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class StockMutations
{
    /// <summary>
    ///     Importa as cotações diárias da B3 (série histórica COTAHIST) de um dia, de um mês ou de um ano, só dos mercados à vista e fracionário. Grava por chave natural: o papel, o pregão e o mercado, então repetir a importação atualiza em vez de duplicar.
    ///
    ///     É uma operação de manutenção, pesada, e hoje não exige autenticação: qualquer chamador pode dispará-la. Um ano inteiro leva minutos.
    ///
    ///     Erro (`extensions.code`): `stock.quoteperiod.invalid`, quando o ano é anterior a 1986 ou o mês está fora de 1 a 12. Informar mais de um campo do período, ou nenhum, é recusado antes de executar.
    /// </summary>
    /// <param name="input">Período a importar: exatamente um entre dia, mês e ano.</param>
    [GraphQLName("importDailyQuotes")]
    public Task<DailyQuoteImportResult> ImportDailyQuotesAsync(
        ImportDailyQuotesRequest input,
        IImportDailyQuotesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
