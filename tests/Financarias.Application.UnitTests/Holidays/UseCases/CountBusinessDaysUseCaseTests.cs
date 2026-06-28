using Financarias.Application.Common.Messaging;
using Financarias.Application.Holidays.Queries;
using Financarias.Application.Holidays.UseCases;
using Financarias.Domain.Holidays.Models;
using NSubstitute;

namespace Financarias.Application.UnitTests.Holidays.UseCases;

public class CountBusinessDaysUseCaseTests
{
    private readonly IQueryHandler<FindHolidaysInRangeQuery, IReadOnlySet<DateOnly>> _handler;
    private readonly CountBusinessDaysUseCase _useCase;

    public CountBusinessDaysUseCaseTests()
    {
        _handler = Substitute.For<IQueryHandler<FindHolidaysInRangeQuery, IReadOnlySet<DateOnly>>>();
        _useCase = new CountBusinessDaysUseCase(_handler);
    }

    [Fact(DisplayName = "Desconta fins de semana e os feriados do range ao contar dias úteis")]
    public async Task Execute_SubtractsWeekendsAndHolidays_FromBusinessDayCount()
    {
        // Arrange — seg 01/07 a sex 05/07 (2024); intervalo (start, end] = ter..sex = 4 dias úteis.
        // Com feriado na quarta (03/07), sobram 3.
        var start = new DateOnly(2024, 7, 1);
        var end = new DateOnly(2024, 7, 5);
        IReadOnlySet<DateOnly> holidays = new HashSet<DateOnly> { new(2024, 7, 3) };
        _handler
            .HandleAsync(Arg.Any<FindHolidaysInRangeQuery>(), Arg.Any<CancellationToken>())
            .Returns(holidays);

        // Act
        var count = await _useCase.ExecuteAsync(start, end, CountryCode.BR);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact(DisplayName = "Repassa o range e o país recebidos para a query do handler")]
    public async Task Execute_PassesRangeAndCountry_ToHandlerQuery()
    {
        // Arrange
        var start = new DateOnly(2024, 7, 1);
        var end = new DateOnly(2024, 7, 5);
        _handler
            .HandleAsync(Arg.Any<FindHolidaysInRangeQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<DateOnly>)new HashSet<DateOnly>());

        // Act
        await _useCase.ExecuteAsync(start, end, CountryCode.BR);

        // Assert
        await _handler.Received(1).HandleAsync(
            new FindHolidaysInRangeQuery(start, end, CountryCode.BR),
            Arg.Any<CancellationToken>());
    }
}
