using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.CorporateEvents.Commands;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Domain.MarketData.CorporateEvents;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.CorporateEvents.Commands;

public class ImportCorporateEventsCommandHandlerTests
{
    private static readonly DateOnly CumDate = new(2026, 8, 21);

    private static readonly ImportCorporateEventsCommand Command = new("PETR", ["PETROBRAS"]);

    private readonly ICorporateEventProvider _provider = Substitute.For<ICorporateEventProvider>();
    private readonly IRepository<CorporateEvent> _events = Substitute.For<IRepository<CorporateEvent>>();

    [Fact(DisplayName = "Insere todos os eventos quando nada existe ainda")]
    public async Task Handle_CreatesEverything_WhenNothingExists()
    {
        // Arrange
        ProviderReturns(skipped: 2, Cash(0.47m), Split());
        ExistingEvents();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 2, 2, 2, 0), result);
        await _events.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<CorporateEvent>>(added => added.Count() == 2 && added.All(e => e.IssuerCode == "PETR")),
            Arg.Any<CancellationToken>());
        await _events.DidNotReceive().DeleteRangeAsync(Arg.Any<IEnumerable<CorporateEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Não toca no banco quando o conjunto publicado é igual ao gravado")]
    public async Task Handle_ChangesNothing_WhenSetsAreEqual()
    {
        // Arrange
        ExistingEvents(Stored(Cash(0.47m)), Stored(Split()));
        ProviderReturns(skipped: 0, Cash(0.47m), Split());
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 2, 0, 0, 0), result);
        await _events.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<CorporateEvent>>(), Arg.Any<CancellationToken>());
        await _events.DidNotReceive().DeleteRangeAsync(Arg.Any<IEnumerable<CorporateEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Troca só o que mudou: cria o novo e remove o que a B3 não publica mais")]
    public async Task Handle_CreatesAndRemovesOnlyTheDifference()
    {
        // Arrange
        var stale = Stored(Cash(0.40m));
        ExistingEvents(stale, Stored(Split()));
        ProviderReturns(skipped: 0, Cash(0.47m), Split());
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 2, 0, 1, 1), result);
        await _events.Received(1).DeleteRangeAsync(
            Arg.Is<IEnumerable<CorporateEvent>>(removed => removed.Single() == stale),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Parcelas idênticas contam uma a uma: duas publicadas e uma gravada cria uma")]
    public async Task Handle_TreatsIdenticalInstallmentsAsAMultiset_Creating()
    {
        // Arrange
        ExistingEvents(Stored(Cash(0.55m)));
        ProviderReturns(skipped: 0, Cash(0.55m), Cash(0.55m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 2, 0, 1, 0), result);
    }

    [Fact(DisplayName = "Parcelas idênticas contam uma a uma: uma publicada e duas gravadas remove uma")]
    public async Task Handle_TreatsIdenticalInstallmentsAsAMultiset_Removing()
    {
        // Arrange
        ExistingEvents(Stored(Cash(0.55m)), Stored(Cash(0.55m)));
        ProviderReturns(skipped: 0, Cash(0.55m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 1, 0, 0, 1), result);
        await _events.Received(1).DeleteRangeAsync(
            Arg.Is<IEnumerable<CorporateEvent>>(removed => removed.Count() == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Quando a B3 não devolve evento nenhum, mantém o que já estava gravado")]
    public async Task Handle_KeepsExistingEvents_WhenProviderReturnsNothing()
    {
        // Arrange
        ExistingEvents(Stored(Cash(0.47m)));
        ProviderReturns(skipped: 3);
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 0, 3, 0, 0), result);
        await _events.DidNotReceive().DeleteRangeAsync(Arg.Any<IEnumerable<CorporateEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Repassa ao provedor o emissor e todos os nomes de pregão")]
    public async Task Handle_PassesTheIssuerAndAllNamesToTheProvider()
    {
        // Arrange
        ProviderReturns(skipped: 0);
        ExistingEvents();
        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(new ImportCorporateEventsCommand("SDIA", ["SADIA S.A.", "SADIA S/A"]));

        // Assert
        await _provider.Received(1).GetAsync(
            "SDIA",
            Arg.Is<IReadOnlyCollection<string>>(names => names.SequenceEqual(new[] { "SADIA S.A.", "SADIA S/A" })),
            Arg.Any<CancellationToken>());
    }

    private ImportCorporateEventsCommandHandler CreateHandler() => new(_provider, _events);

    private static CorporateEventImportItem Cash(decimal amount) =>
        new(null, "ON", CorporateEventKind.CashDividend, CumDate, null, null, amount, 1, null, "DIVIDENDO");

    private static CorporateEventImportItem Split() =>
        new("BRPETRACNOR9", "ON", CorporateEventKind.StockSplit, new DateOnly(2008, 4, 25), null, null, null, null, 100m, "DESDOBRAMENTO");

    private static CorporateEvent Stored(CorporateEventImportItem item) =>
        CorporateEvent.Create(
            "PETR", item.Isin, item.ShareClass, item.Kind, item.CumDate, item.ApprovalDate, item.PaymentDate,
            item.Amount, item.PerShares, item.Factor, item.Label);

    private void ProviderReturns(int skipped, params CorporateEventImportItem[] items) =>
        _provider.GetAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new CorporateEventFetch(items, skipped));

    private void ExistingEvents(params CorporateEvent[] events) =>
        _events.ListAsync(Arg.Any<ISpecification<CorporateEvent>>(), Arg.Any<CancellationToken>())
            .Returns(events.ToList());
}
