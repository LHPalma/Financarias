using Financarias.Application.Holidays.Import;
using Financarias.Application.Holidays.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class HolidayMutations
{
    [GraphQLName("importHolidays")]
    public Task<HolidayImportResult> ImportHolidaysAsync(
        IImportHolidaysUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}
