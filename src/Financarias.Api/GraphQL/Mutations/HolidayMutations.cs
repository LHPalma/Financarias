using Financarias.Application.Holidays.Import;
using Financarias.Application.Holidays.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class HolidayMutations
{
    /// <summary>
    ///     Importa os feriados nacionais de duas fontes e grava só os que ainda não existem, sem duplicar por data e país: a planilha da ANBIMA, de 2001 em diante, e a BrasilAPI, que cobre de 1980 a 2000. Na mesma data vale a primeira fonte. A BrasilAPI calcula os feriados por regra, aplicando a lei de hoje a anos antigos, e de 1986 a 1990 marca como feriado dez dias em que a bolsa operou (Corpus Christi, Tiradentes, Aparecida e Finados).
    ///
    ///     É uma operação de manutenção e, hoje, não exige autenticação: qualquer chamador pode dispará-la.
    /// </summary>
    [GraphQLName("importHolidays")]
    public Task<HolidayImportResult> ImportHolidaysAsync(
        IImportHolidaysUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}
