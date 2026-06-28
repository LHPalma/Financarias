using Financarias.Application.Holidays.Import;
using Financarias.Application.Holidays.UseCases;

namespace Financarias.Api.GraphQL;

public class Mutation
{
    [GraphQLName("importHolidays")]
    public Task<HolidayImportResult> ImportHolidaysAsync(
        IImportHolidaysUseCase useCase,
        CancellationToken cancellationToken)
    {
        return useCase.ExecuteAsync(cancellationToken);
    }
}