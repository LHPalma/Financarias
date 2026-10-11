using Financarias.Application.MarketData.Indexes.Commands;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Domain.MarketData.Indexes;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class ImportIndexQuotesHandlerTests : IAsyncLifetime
{
    private static readonly DateOnly Day1 = new(2018, 10, 5);
    private static readonly DateOnly Day2 = new(2018, 10, 8);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Import insere os fechamentos e faz upsert real na reimportação (insere, atualiza e pula)")]
    public async Task Import_PersistsThenUpserts_AgainstRealPostgres()
    {
        // Arrange & Act — primeira importação: um pregão
        var first = await RunImportAsync(2018, Item(Day1, 82_321.52m));

        // Assert — tudo novo
        Assert.Equal(new IndexQuoteImportResult(1, 1, 0), first);

        // Act — reimportação: o pregão 1 mudou de valor e chega o pregão 2
        var second = await RunImportAsync(2018, Item(Day1, 82_321.99m), Item(Day2, 86_083.92m));

        // Assert — um novo, um atualizado
        Assert.Equal(new IndexQuoteImportResult(2, 1, 1), second);

        // Act — terceira rodada idêntica à segunda: nada muda
        var third = await RunImportAsync(2018, Item(Day1, 82_321.99m), Item(Day2, 86_083.92m));

        // Assert
        Assert.Equal(new IndexQuoteImportResult(2, 0, 0), third);

        await using var verify = CreateContext();
        Assert.Equal(2, await verify.IndexQuotes.CountAsync());
        Assert.Equal(82_321.99m, (await verify.IndexQuotes.SingleAsync(q => q.TradingDate == Day1)).CloseValue);
    }

    [Fact(DisplayName = "A specification só carrega o ano e o índice pedidos, sem tocar em outros anos")]
    public async Task Import_DoesNotTouchOtherYears()
    {
        // Arrange — um fechamento de 2017 já existe
        await RunImportAsync(2017, Item(new DateOnly(2017, 10, 5), 75_000.10m));

        // Act — importa 2018
        var result = await RunImportAsync(2018, Item(Day1, 82_321.52m));

        // Assert
        Assert.Equal(new IndexQuoteImportResult(1, 1, 0), result);

        await using var verify = CreateContext();
        Assert.Equal(2, await verify.IndexQuotes.CountAsync());
        Assert.Equal(
            75_000.10m,
            (await verify.IndexQuotes.SingleAsync(q => q.TradingDate == new DateOnly(2017, 10, 5))).CloseValue);
    }

    private static IndexQuoteImportItem Item(DateOnly tradingDate, decimal closeValue) =>
        new(MarketIndex.Ibovespa, tradingDate, closeValue, IndexQuoteSource.B3);

    private async Task<IndexQuoteImportResult> RunImportAsync(int year, params IndexQuoteImportItem[] items)
    {
        await using var context = CreateContext();
        var handler = new ImportIndexQuotesCommandHandler(new StubProvider(items), new Repository<IndexQuote>(context));

        return await handler.HandleAsync(new ImportIndexQuotesCommand(MarketIndex.Ibovespa, year));
    }

    private sealed class StubProvider(IReadOnlyList<IndexQuoteImportItem> items) : IIndexQuoteProvider
    {
        public Task<IReadOnlyList<IndexQuoteImportItem>> GetYearAsync(
            MarketIndex marketIndex,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(items);
    }

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }
}
