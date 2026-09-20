using Financarias.Application.Holidays.UseCases;
using Financarias.Domain.Holidays.Models;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class HolidayQueries
{
    /// <summary>
    ///     Conta os dias úteis no intervalo (start, end]: o dia inicial fica de fora e o final entra. Fins de semana e os feriados do país já importados são descontados. Não exige autenticação.
    /// </summary>
    /// <param name="start">Data inicial, exclusive.</param>
    /// <param name="end">Data final, inclusive.</param>
    /// <param name="countryCode">País do calendário de feriados.</param>
    [GraphQLName("businessDayCount")]
    public Task<int> BusinessDayCountAsync(
        DateOnly start,
        DateOnly end,
        CountryCode countryCode,
        ICountBusinessDaysUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(start, end, countryCode, cancellationToken);
}
