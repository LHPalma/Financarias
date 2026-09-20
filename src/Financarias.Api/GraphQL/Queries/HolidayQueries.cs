using Financarias.Application.Holidays.UseCases;
using Financarias.Domain.Holidays.Models;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class HolidayQueries
{
    [GraphQLName("businessDayCount")]
    public Task<int> BusinessDayCountAsync(
        DateOnly start,
        DateOnly end,
        CountryCode countryCode,
        ICountBusinessDaysUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(start, end, countryCode, cancellationToken);
}
