using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Indicators.Commands;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Indicators.Commands;

public class ImportIndicatorValuesCommandHandlerTests
{
    private static readonly DateOnly Friday = new(2026, 10, 9);

    private static readonly ImportIndicatorValuesCommand Command = new(EconomicIndicator.Dollar, 2026);

    private readonly IIndicatorValueProvider _provider = Substitute.For<IIndicatorValueProvider>();
    private readonly IRepository<IndicatorValue> _values = Substitute.For<IRepository<IndicatorValue>>();

    [Fact(DisplayName = "Devolve resultado zerado sem tocar no banco quando o ano não tem valores")]
    public async Task Handle_ReturnsZeroes_WhenProviderHasNoData()
    {
        // Arrange
        ProviderReturns();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndicatorValueImportResult(0, 0, 0), result);
        await _values.DidNotReceive().ListAsync(Arg.Any<ISpecification<IndicatorValue>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Insere os valores quando nada existe ainda")]
    public async Task Handle_CreatesValues_WhenNothingExists()
    {
        // Arrange
        ProviderReturns(Item(Friday, 4.9892m), Item(Friday.AddDays(-1), 5.0119m));
        ExistingValues();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndicatorValueImportResult(2, 2, 0), result);
        await _values.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<IndicatorValue>>(added => added.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Atualiza o valor existente quando o número mudou")]
    public async Task Handle_UpdatesValue_WhenNumberChanged()
    {
        // Arrange
        var existing = Value(Friday, 4.9000m);
        ExistingValues(existing);
        ProviderReturns(Item(Friday, 4.9892m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndicatorValueImportResult(1, 0, 1), result);
        Assert.Equal(4.9892m, existing.Value);
        await _values.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _values.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<IndicatorValue>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Ignora o valor idêntico (nem insere nem atualiza)")]
    public async Task Handle_SkipsValue_WhenUnchanged()
    {
        // Arrange
        ExistingValues(Value(Friday, 4.9892m));
        ProviderReturns(Item(Friday, 4.9892m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndicatorValueImportResult(1, 0, 0), result);
        await _values.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<IndicatorValue>>(), Arg.Any<CancellationToken>());
        await _values.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Insere o novo e atualiza o existente na mesma importação")]
    public async Task Handle_CreatesAndUpdates_InTheSameImport()
    {
        // Arrange
        var existing = Value(Friday, 4.9000m);
        ExistingValues(existing);
        ProviderReturns(Item(Friday, 4.9892m), Item(Friday.AddDays(-1), 5.0119m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndicatorValueImportResult(2, 1, 1), result);
        Assert.Equal(4.9892m, existing.Value);
    }

    private ImportIndicatorValuesCommandHandler CreateHandler() => new(_provider, _values);

    private static IndicatorValue Value(DateOnly referenceDate, decimal value) =>
        IndicatorValue.Create(EconomicIndicator.Dollar, referenceDate, value);

    private static IndicatorValueImportItem Item(DateOnly referenceDate, decimal value) =>
        new(EconomicIndicator.Dollar, referenceDate, value);

    private void ProviderReturns(params IndicatorValueImportItem[] items) =>
        _provider.GetYearAsync(Arg.Any<EconomicIndicator>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(items);

    private void ExistingValues(params IndicatorValue[] values) =>
        _values.ListAsync(Arg.Any<ISpecification<IndicatorValue>>(), Arg.Any<CancellationToken>())
            .Returns(values.ToList());
}
