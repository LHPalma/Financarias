using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.IndexComposition.Commands;
using Financarias.Application.MarketData.IndexComposition.Import;
using Financarias.Domain.MarketData.IndexComposition;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.IndexComposition.Commands;

public class ImportIndexCompositionFileCommandHandlerTests
{
    private const string Url = "https://www.b3.com.br/data/files/AA/BB/prv.zip";

    private static readonly ImportIndexCompositionFileCommand Command = new(Url);

    private readonly IIndexCompositionProvider _provider = Substitute.For<IIndexCompositionProvider>();
    private readonly IRepository<IndexCompositionItem> _items = Substitute.For<IRepository<IndexCompositionItem>>();

    [Fact(DisplayName = "Devolve resultado zerado sem tocar no banco quando o arquivo não tem carteira")]
    public async Task Handle_ReturnsZeroes_WhenFileHasNoPortfolio()
    {
        // Arrange
        ProviderReturns();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexCompositionFileImportResult(0, 0, 0, 0), result);
        await _items.DidNotReceive().ListAsync(Arg.Any<ISpecification<IndexCompositionItem>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Insere as linhas de cada carteira quando nada existe ainda")]
    public async Task Handle_CreatesItems_WhenNothingExists()
    {
        // Arrange
        ProviderReturns(Portfolio("IBOV", Row("ABEV3", 100, 6.3m), Row("VALE3", 200, 9.1m)), Portfolio("IBXL", Row("ABEV3", 100, 3.1m)));
        ExistingItems();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexCompositionFileImportResult(2, 3, 3, 0), result);
        await _items.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<IndexCompositionItem>>(added => added.Count() == 3 && added.All(item => item.SourceUrl == Url)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Atualiza a linha existente quando a quantidade mudou")]
    public async Task Handle_UpdatesItem_WhenQuantityChanged()
    {
        // Arrange
        var existing = Item("IBOV", "ABEV3", 90, 6.3m);
        ExistingItems(existing);
        ProviderReturns(Portfolio("IBOV", Row("ABEV3", 100, 6.3m)));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexCompositionFileImportResult(1, 1, 0, 1), result);
        Assert.Equal(100, existing.TheoreticalQuantity);
        await _items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _items.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<IndexCompositionItem>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Ignora a linha idêntica (nem insere nem atualiza)")]
    public async Task Handle_SkipsItem_WhenUnchanged()
    {
        // Arrange
        ExistingItems(Item("IBOV", "ABEV3", 100, 6.3m));
        ProviderReturns(Portfolio("IBOV", Row("ABEV3", 100, 6.3m)));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexCompositionFileImportResult(1, 1, 0, 0), result);
        await _items.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<IndexCompositionItem>>(), Arg.Any<CancellationToken>());
        await _items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Trata o mesmo ativo repetido na carteira como atualização, não como duplicata")]
    public async Task Handle_TreatsRepeatedTicker_AsUpdate()
    {
        // Arrange
        ExistingItems();
        ProviderReturns(Portfolio("IBOV", Row("ABEV3", 100, 6.3m), Row("ABEV3", 150, 6.3m)));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new IndexCompositionFileImportResult(1, 2, 1, 1), result);
    }

    private ImportIndexCompositionFileCommandHandler CreateHandler() => new(_provider, _items);

    private static IndexCompositionRowImportItem Row(string ticker, long quantity, decimal weight) =>
        new(ticker, ticker, "ON NM", quantity, weight);

    private static IndexPortfolioImportItem Portfolio(string indexCode, params IndexCompositionRowImportItem[] rows) =>
        new(indexCode, 2018, 3, IndexPortfolioKind.FirstPreview, new DateOnly(2018, 7, 31), 16804141.7293524m, rows);

    private static IndexCompositionItem Item(string indexCode, string ticker, long quantity, decimal weight) =>
        IndexCompositionItem.Create(
            indexCode, 2018, 3, IndexPortfolioKind.FirstPreview, ticker, ticker, "ON NM", quantity, weight,
            new DateOnly(2018, 7, 31), 16804141.7293524m, Url);

    private void ProviderReturns(params IndexPortfolioImportItem[] portfolios) =>
        _provider.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(portfolios);

    private void ExistingItems(params IndexCompositionItem[] items) =>
        _items.ListAsync(Arg.Any<ISpecification<IndexCompositionItem>>(), Arg.Any<CancellationToken>())
            .Returns(items.ToList());
}
