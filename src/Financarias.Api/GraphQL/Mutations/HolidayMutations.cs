using Financarias.Application.Holidays.Import;
using Financarias.Application.Holidays.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class HolidayMutations
{
    /// <summary>
    ///     Importa os feriados nacionais do provedor (ANBIMA) e grava só os que ainda não existem, sem duplicar por data e país.
    ///
    ///     É uma operação de manutenção e, hoje, não exige autenticação: qualquer chamador pode dispará-la.
    /// </summary>
    [GraphQLName("importHolidays")]
    public Task<HolidayImportResult> ImportHolidaysAsync(
        IImportHolidaysUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}
