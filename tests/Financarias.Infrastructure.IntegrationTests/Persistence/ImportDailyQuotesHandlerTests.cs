using System.Runtime.CompilerServices;
using Financarias.Application.MarketData.Stocks.Commands;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class ImportDailyQuotesHandlerTests : IAsyncLifetime
{
    private static readonly DateOnly Day1 = new(2026, 10, 8);
    private static readonly DateOnly Day2 = new(2026, 10, 9);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Import insere cotações novas e faz upsert real na reimportação (insere, atualiza e pula)")]
    public async Task Import_PersistsThenUpserts_AgainstRealPostgres()
    {
        // Arrange & Act — primeira importação: dois papéis no pregão 1
        var first = await RunImportAsync(
            Item("PETR4", Day1, closePrice: 38.70m),
            Item("VALE3", Day1, closePrice: 68.00m));

        // Assert — tudo novo
        Assert.Equal(new DailyQuoteImportResult(true, 2, 0, 2, 0), first);

        // Act — reimportação: PETR4 mudou de fechamento, VALE3 igual, e chega o pregão 2
        var second = await RunImportAsync(
            Item("PETR4", Day1, closePrice: 39.10m), // mudou (update)
            Item("VALE3", Day1, closePrice: 68.00m), // igual (pula)
            Item("PETR4", Day2, closePrice: 39.50m)); // pregão novo (insert)

        // Assert — uma nova, uma atualizada
        Assert.Equal(new DailyQuoteImportResult(true, 3, 0, 1, 1), second);

        // Assert — estado final do banco
        await using var verify = CreateContext();
        Assert.Equal(3, await verify.DailyQuotes.CountAsync());

        var updated = await verify.DailyQuotes
            .SingleAsync(q => q.Ticker == Ticker.Create("PETR4") && q.TradingDate == Day1);
        Assert.Equal(39.10m, updated.ClosePrice);
    }

    [Fact(DisplayName = "Import sem arquivo devolve resultado vazio e não toca no banco")]
    public async Task Import_ReturnsFileNotFound_WhenProviderHasNoFile()
    {
        // Act
        var result = await RunImportAsync(file: null);

        // Assert
        Assert.Equal(new DailyQuoteImportResult(false, 0, 0, 0, 0), result);

        await using var verify = CreateContext();
        Assert.Equal(0, await verify.DailyQuotes.CountAsync());
    }

    private static DailyQuoteImportItem Item(string ticker, DateOnly tradingDate, decimal closePrice) =>
        new(
            Ticker.Create(ticker),
            tradingDate,
            QuoteMarketType.Cash,
            "02",
            "EMPRESA",
            "ON",
            "BRXXXXACNOR0",
            38.10m,
            68.90m,
            37.80m,
            38.40m,
            closePrice,
            52_310,
            41_250_300,
            1_583_000_000.55m,
            1);

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }

    private Task<DailyQuoteImportResult> RunImportAsync(params DailyQuoteImportItem[] items) =>
        RunImportAsync(new StubFile(items));

    private async Task<DailyQuoteImportResult> RunImportAsync(IDailyQuoteFile? file)
    {
        await using var context = CreateContext();
        var handler = new ImportDailyQuotesCommandHandler(
            new StubProvider(file),
            new Repository<DailyQuote>(context),
            new UnitOfWork(context));

        return await handler.HandleAsync(new ImportDailyQuotesCommand(QuotePeriod.ForDay(Day2)));
    }

    private sealed class StubProvider(IDailyQuoteFile? file) : IDailyQuoteProvider
    {
        public Task<IDailyQuoteFile?> OpenAsync(QuotePeriod period, CancellationToken cancellationToken = default) =>
            Task.FromResult(file);
    }

    private sealed class StubFile(IReadOnlyList<DailyQuoteImportItem> items) : IDailyQuoteFile
    {
        public int RowsSkipped => 0;

        public async IAsyncEnumerable<DailyQuoteImportItem> ReadAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
