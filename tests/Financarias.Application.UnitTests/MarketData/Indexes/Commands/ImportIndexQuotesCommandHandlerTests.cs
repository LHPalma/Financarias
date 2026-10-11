using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Indexes.Commands;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Domain.MarketData.Indexes;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Indexes.Commands;

public class ImportIndexQuotesCommandHandlerTests
{
    private static readonly DateOnly Monday = new(2018, 10, 8);

    private static readonly ImportIndexQuotesCommand Command = new(MarketIndex.Ibovespa, 2018);

    private readonly IIndexQuoteProvider _provider = Substitute.For<IIndexQuoteProvider>();
    private readonly IRepository<IndexQuote> _quotes = Substitute.For<IRepository<IndexQuote>>();

    [Fact(DisplayName = "Devolve resultado zerado sem tocar no banco quando o ano não tem pregões")]
    public async Task Handle_ReturnsZeroes_WhenProviderHasNoData()
    {
        // Arrange
        ProviderReturns();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexQuoteImportResult(0, 0, 0), result);
        await _quotes.DidNotReceive().ListAsync(Arg.Any<ISpecification<IndexQuote>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Insere os fechamentos quando nada existe ainda")]
    public async Task Handle_CreatesQuotes_WhenNothingExists()
    {
        // Arrange
        ProviderReturns(Item(Monday, 86_083.92m), Item(Monday.AddDays(1), 86_087.50m));
        NoExistingQuotes();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexQuoteImportResult(2, 2, 0), result);
        await _quotes.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<IndexQuote>>(added => added.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Atualiza o fechamento existente quando o valor mudou")]
    public async Task Handle_UpdatesQuote_WhenValueChanged()
    {
        // Arrange
        var existing = Quote(Monday, 86_083.00m);
        ExistingQuotes(existing);
        ProviderReturns(Item(Monday, 86_083.92m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexQuoteImportResult(1, 0, 1), result);
        Assert.Equal(86_083.92m, existing.CloseValue);
        await _quotes.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _quotes.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<IndexQuote>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Ignora o fechamento idêntico (nem insere nem atualiza)")]
    public async Task Handle_SkipsQuote_WhenUnchanged()
    {
        // Arrange
        ExistingQuotes(Quote(Monday, 86_083.92m));
        ProviderReturns(Item(Monday, 86_083.92m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexQuoteImportResult(1, 0, 0), result);
        await _quotes.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<IndexQuote>>(), Arg.Any<CancellationToken>());
        await _quotes.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Insere o novo e atualiza o existente na mesma importação")]
    public async Task Handle_CreatesAndUpdates_InTheSameImport()
    {
        // Arrange
        var existing = Quote(Monday, 86_000.00m);
        ExistingQuotes(existing);
        ProviderReturns(Item(Monday, 86_083.92m), Item(Monday.AddDays(1), 86_087.50m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexQuoteImportResult(2, 1, 1), result);
        Assert.Equal(86_083.92m, existing.CloseValue);
    }

    private ImportIndexQuotesCommandHandler CreateHandler() => new(_provider, _quotes);

    private static IndexQuote Quote(DateOnly tradingDate, decimal closeValue) =>
        IndexQuote.Create(MarketIndex.Ibovespa, tradingDate, closeValue, IndexQuoteSource.B3);

    private static IndexQuoteImportItem Item(DateOnly tradingDate, decimal closeValue) =>
        new(MarketIndex.Ibovespa, tradingDate, closeValue, IndexQuoteSource.B3);

    private void ProviderReturns(params IndexQuoteImportItem[] items) =>
        _provider.GetYearAsync(Arg.Any<MarketIndex>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(items);

    private void NoExistingQuotes() => ExistingQuotes();

    private void ExistingQuotes(params IndexQuote[] quotes) =>
        _quotes.ListAsync(Arg.Any<ISpecification<IndexQuote>>(), Arg.Any<CancellationToken>())
            .Returns(quotes.ToList());
}
