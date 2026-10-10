using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Stocks.Commands;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Stocks.Commands;

public class ImportDailyQuotesCommandHandlerTests
{
    private static readonly DateOnly TradingDate = new(2026, 10, 9);

    private static readonly ImportDailyQuotesCommand Command = new(QuotePeriod.ForDay(TradingDate));

    private readonly IDailyQuoteProvider _provider = Substitute.For<IDailyQuoteProvider>();
    private readonly IDailyQuoteFile _file = Substitute.For<IDailyQuoteFile>();
    private readonly IRepository<DailyQuote> _quotes = Substitute.For<IRepository<DailyQuote>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact(DisplayName = "Devolve resultado zerado e arquivo não encontrado quando o provedor não tem o arquivo")]
    public async Task Handle_ReturnsFileNotFound_WhenProviderHasNoFile()
    {
        // Arrange
        _provider.OpenAsync(Arg.Any<QuotePeriod>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDailyQuoteFile?>(null));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new DailyQuoteImportResult(false, 0, 0, 0, 0), result);
        await _quotes.DidNotReceive().ListAsync(Arg.Any<ISpecification<DailyQuote>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Insere as cotações quando nada existe ainda e repassa as linhas puladas do arquivo")]
    public async Task Handle_CreatesQuotes_WhenNothingExists()
    {
        // Arrange
        FileReturns(rowsSkipped: 3, Item("PETR4"), Item("VALE3"));
        NoExistingQuotes();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new DailyQuoteImportResult(true, 2, 3, 2, 0), result);
        await _quotes.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<DailyQuote>>(added => added.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Atualiza a cotação existente quando os valores mudaram")]
    public async Task Handle_UpdatesQuote_WhenValuesChanged()
    {
        // Arrange
        var existing = Quote("PETR4", closePrice: 38.70m);
        ExistingQuotes(existing);
        FileReturns(rowsSkipped: 0, Item("PETR4", closePrice: 39.10m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new DailyQuoteImportResult(true, 1, 0, 0, 1), result);
        Assert.Equal(39.10m, existing.ClosePrice);
        await _quotes.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _quotes.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<DailyQuote>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Ignora a cotação idêntica (nem insere nem atualiza)")]
    public async Task Handle_SkipsQuote_WhenUnchanged()
    {
        // Arrange
        ExistingQuotes(Quote("PETR4"));
        FileReturns(rowsSkipped: 0, Item("PETR4"));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(new DailyQuoteImportResult(true, 1, 0, 0, 0), result);
        await _quotes.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<DailyQuote>>(), Arg.Any<CancellationToken>());
        await _quotes.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Trata a mesma chave repetida no lote como uma só cotação")]
    public async Task Handle_CreatesOnce_WhenSameKeyAppearsTwiceInBatch()
    {
        // Arrange
        FileReturns(rowsSkipped: 0, Item("PETR4"), Item("PETR4"));
        NoExistingQuotes();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(2, result.RowsRead);
        Assert.Equal(1, result.QuotesCreated);
        Assert.Equal(0, result.QuotesUpdated);
        await _quotes.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<DailyQuote>>(added => added.Count() == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Processa em lotes de 5.000 e limpa o rastreamento ao fim de cada lote")]
    public async Task Handle_FlushesInBatches_AndClearsTracking()
    {
        // Arrange: 5.001 itens (mesmo papel, pregões diferentes) -> 2 lotes
        var items = Enumerable.Range(0, 5001)
            .Select(offset => Item("PETR4", tradingDate: TradingDate.AddDays(-offset)))
            .ToArray();
        FileReturns(rowsSkipped: 0, items);
        NoExistingQuotes();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command);

        // Assert
        Assert.Equal(5001, result.RowsRead);
        Assert.Equal(5001, result.QuotesCreated);
        await _quotes.Received(2).AddRangeAsync(Arg.Any<IEnumerable<DailyQuote>>(), Arg.Any<CancellationToken>());
        _unitOfWork.Received(2).ClearTracking();
    }

    [Fact(DisplayName = "Descarta o arquivo ao terminar o import")]
    public async Task Handle_DisposesFile_WhenDone()
    {
        // Arrange
        FileReturns(rowsSkipped: 0, Item("PETR4"));
        NoExistingQuotes();
        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(Command);

        // Assert
        await _file.Received(1).DisposeAsync();
    }

    private ImportDailyQuotesCommandHandler CreateHandler() => new(_provider, _quotes, _unitOfWork);

    private static async IAsyncEnumerable<DailyQuoteImportItem> ToAsync(DailyQuoteImportItem[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }

    private static DailyQuote Quote(string ticker, decimal closePrice = 38.70m) =>
        DailyQuote.Create(
            Ticker.Create(ticker),
            TradingDate,
            QuoteMarketType.Cash,
            "02",
            "EMPRESA",
            "ON",
            "BRXXXXACNOR0",
            38.10m,
            38.90m,
            37.80m,
            38.40m,
            closePrice,
            52_310,
            41_250_300,
            1_583_000_000.55m,
            1);

    private static DailyQuoteImportItem Item(string ticker, decimal closePrice = 38.70m, DateOnly? tradingDate = null) =>
        new(
            Ticker.Create(ticker),
            tradingDate ?? TradingDate,
            QuoteMarketType.Cash,
            "02",
            "EMPRESA",
            "ON",
            "BRXXXXACNOR0",
            38.10m,
            38.90m,
            37.80m,
            38.40m,
            closePrice,
            52_310,
            41_250_300,
            1_583_000_000.55m,
            1);

    private void FileReturns(int rowsSkipped, params DailyQuoteImportItem[] items)
    {
        _file.RowsSkipped.Returns(rowsSkipped);
        _file.ReadAsync(Arg.Any<CancellationToken>()).Returns(ToAsync(items));
        _provider.OpenAsync(Arg.Any<QuotePeriod>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDailyQuoteFile?>(_file));
    }

    private void NoExistingQuotes() =>
        _quotes.ListAsync(Arg.Any<ISpecification<DailyQuote>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DailyQuote>());

    private void ExistingQuotes(params DailyQuote[] quotes) =>
        _quotes.ListAsync(Arg.Any<ISpecification<DailyQuote>>(), Arg.Any<CancellationToken>())
            .Returns(quotes.ToList());
}
