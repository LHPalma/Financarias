using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Inflation.Commands;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Holidays.Models;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Domain.MarketData.Inflation;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Inflation.Commands;

public class RebuildInflationLevelsCommandHandlerTests
{
    private static readonly RebuildInflationLevelsCommand Command = new();

    private readonly IRepository<IndicatorValue> _indicatorValues = Substitute.For<IRepository<IndicatorValue>>();
    private readonly IRepository<Holiday> _holidays = Substitute.For<IRepository<Holiday>>();
    private readonly IRepository<InflationLevel> _levels = Substitute.For<IRepository<InflationLevel>>();

    [Fact(DisplayName = "Cria um nível por dia corrido, do primeiro dia 15 ao dia 15 seguinte ao último mês")]
    public async Task Handle_CreatesOneLevelPerCalendarDay()
    {
        // Arrange
        GivenIpca(10m, 20m, 5m);
        GivenNoHolidays();
        GivenExistingLevels();
        List<InflationLevel>? added = null;
        await _levels.AddRangeAsync(Arg.Do<IEnumerable<InflationLevel>>(items => added = items.ToList()), Arg.Any<CancellationToken>());

        // Act
        var result = await CreateHandler().HandleAsync(Command);

        // Assert: 15/01 a 15/04/2024 tem 92 dias
        Assert.Equal(new InflationLevelsRebuildResult(new DateOnly(2024, 1, 15), new DateOnly(2024, 4, 15), 92, 92, 0, 0), result);
        Assert.Equal(92, added!.Count);
        Assert.Equal(1m, added.Single(l => l.Day == new DateOnly(2024, 1, 15)).Level);
        Assert.Equal(1.386m, added.Single(l => l.Day == new DateOnly(2024, 4, 15)).Level);
    }

    [Fact(DisplayName = "Usa os feriados: com 15/02 feriado, a janela de fevereiro começa em 16/02")]
    public async Task Handle_UsesTheHolidays()
    {
        // Arrange
        GivenIpca(10m, 20m, 5m);
        GivenHolidays(new DateOnly(2024, 2, 15));
        GivenExistingLevels();
        List<InflationLevel>? added = null;
        await _levels.AddRangeAsync(Arg.Do<IEnumerable<InflationLevel>>(items => added = items.ToList()), Arg.Any<CancellationToken>());

        // Act
        await CreateHandler().HandleAsync(Command);

        // Assert
        Assert.Equal(1.10m, added!.Single(l => l.Day == new DateOnly(2024, 2, 16)).Level);
        Assert.Equal(
            added!.Single(l => l.Day == new DateOnly(2024, 2, 14)).Level,
            added!.Single(l => l.Day == new DateOnly(2024, 2, 15)).Level);
    }

    [Fact(DisplayName = "Não toca no banco quando os níveis gravados já são os calculados")]
    public async Task Handle_ChangesNothing_WhenTheLevelsAreAlreadyRight()
    {
        // Arrange
        GivenIpca(10m, 20m, 5m);
        GivenNoHolidays();
        var computed = await Capture();
        GivenExistingLevels(computed.ToArray());

        // Act
        var result = await CreateHandler().HandleAsync(Command);

        // Assert
        Assert.Equal(new InflationLevelsRebuildResult(new DateOnly(2024, 1, 15), new DateOnly(2024, 4, 15), 92, 0, 0, 0), result);
        await _levels.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<InflationLevel>>(), Arg.Any<CancellationToken>());
        await _levels.DidNotReceive().DeleteRangeAsync(Arg.Any<IEnumerable<InflationLevel>>(), Arg.Any<CancellationToken>());
        await _levels.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Troca só a diferença: atualiza o nível que mudou, cria o que falta e remove o dia que saiu")]
    public async Task Handle_UpdatesCreatesAndRemovesTheDifference()
    {
        // Arrange
        GivenIpca(10m, 20m, 5m);
        GivenNoHolidays();
        var computed = await Capture();

        var wrong = InflationLevel.Create(new DateOnly(2024, 2, 1), 99m);
        var outOfRange = InflationLevel.Create(new DateOnly(2024, 6, 1), 3m);
        var existing = computed
            .Where(l => l.Day != new DateOnly(2024, 1, 20) && l.Day != new DateOnly(2024, 2, 1))
            .Append(wrong)
            .Append(outOfRange)
            .ToArray();
        GivenExistingLevels(existing);

        // Act
        var result = await CreateHandler().HandleAsync(Command);

        // Assert
        Assert.Equal(new InflationLevelsRebuildResult(new DateOnly(2024, 1, 15), new DateOnly(2024, 4, 15), 92, 1, 1, 1), result);
        Assert.NotEqual(99m, wrong.Level);
        await _levels.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<InflationLevel>>(items => items.Single().Day == new DateOnly(2024, 1, 20)),
            Arg.Any<CancellationToken>());
        await _levels.Received(1).DeleteRangeAsync(
            Arg.Is<IEnumerable<InflationLevel>>(items => items.Single() == outOfRange),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Sem IPCA importado lança erro de domínio e não grava nada")]
    public async Task Handle_WithoutIpca_ThrowsDomainError()
    {
        // Arrange
        _indicatorValues.ListAsync(Arg.Any<ISpecification<IndicatorValue>>(), Arg.Any<CancellationToken>()).Returns(new List<IndicatorValue>());
        GivenNoHolidays();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => CreateHandler().HandleAsync(Command));

        Assert.Equal("inflation.series.invalid", exception.Code);
        await _levels.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<InflationLevel>>(), Arg.Any<CancellationToken>());
    }

    private RebuildInflationLevelsCommandHandler CreateHandler() => new(_indicatorValues, _holidays, _levels);

    private async Task<List<InflationLevel>> Capture()
    {
        GivenExistingLevels();
        List<InflationLevel>? added = null;
        await _levels.AddRangeAsync(Arg.Do<IEnumerable<InflationLevel>>(items => added = items.ToList()), Arg.Any<CancellationToken>());
        await CreateHandler().HandleAsync(Command);
        _levels.ClearReceivedCalls();

        return added!;
    }

    private void GivenIpca(params decimal[] ratesFromJanuary2024) =>
        _indicatorValues.ListAsync(Arg.Any<ISpecification<IndicatorValue>>(), Arg.Any<CancellationToken>())
            .Returns(ratesFromJanuary2024
                .Select((rate, i) => IndicatorValue.Create(EconomicIndicator.Ipca, new DateOnly(2024, 1 + i, 1), rate))
                .ToList());

    private void GivenNoHolidays() => GivenHolidays();

    private void GivenHolidays(params DateOnly[] dates) =>
        _holidays.ListAsync(Arg.Any<ISpecification<Holiday>>(), Arg.Any<CancellationToken>())
            .Returns(dates.Select(date => Holiday.Create(date, "Feriado", CountryCode.BR)).ToList());

    private void GivenExistingLevels(params InflationLevel[] levels) =>
        _levels.ListAsync(Arg.Any<CancellationToken>()).Returns(levels.ToList());
}
