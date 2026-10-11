using Financarias.Application.MarketData.CorporateEvents.Commands;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Application.MarketData.CorporateEvents.Queries;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.CorporateEvents;
using Financarias.Domain.MarketData.Stocks;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class CorporateEventsImportTests : IAsyncLifetime
{
    private static readonly DateOnly CumDate = new(2026, 8, 21);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Os nomes de pregão de um emissor saem do prefixo do ISIN, sem misturar emissores")]
    public async Task TradingNames_AreFoundByTheIsinPrefix()
    {
        // Arrange
        await using (var write = CreateContext())
        {
            write.DailyQuotes.Add(Quote("SDIA4", "SADIA S.A.", "BRSDIAACNPR1", new DateOnly(2001, 3, 5)));
            write.DailyQuotes.Add(Quote("SDIA4", "SADIA S/A", "BRSDIAACNPR1", new DateOnly(2005, 3, 7)));
            write.DailyQuotes.Add(Quote("SDIA3", "SADIA S/A", "BRSDIAACNOR3", new DateOnly(2005, 3, 7)));
            write.DailyQuotes.Add(Quote("PETR4", "PETROBRAS", "BRPETRACNPR6", new DateOnly(2005, 3, 7)));
            write.DailyQuotes.Add(Quote("ACES3", "ACESITA", "ACESACON", new DateOnly(1995, 1, 2)));
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var reads = new IssuerReads(read);

        // Act
        var sadia = await reads.TradingNamesAsync("SDIA");
        var petrobras = await reads.TradingNamesAsync("PETR");
        var unknown = await reads.TradingNamesAsync("ZZZZ");
        var oldIsinForm = await reads.TradingNamesAsync("ACES");

        // Assert
        Assert.Equal(["SADIA S.A.", "SADIA S/A"], sadia);
        Assert.Equal(["PETROBRAS"], petrobras);
        Assert.Empty(unknown);
        Assert.Empty(oldIsinForm);
    }

    [Fact(DisplayName = "Import grava, não toca quando é igual e troca só a diferença, contra Postgres real")]
    public async Task Import_PersistsThenDiffs_AgainstRealPostgres()
    {
        // Arrange & Act — primeira importação: duas parcelas idênticas e um desdobramento
        var first = await RunImportAsync(Cash(0.55m), Cash(0.55m), Split());

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 3, 0, 3, 0), first);
        await using (var verify = CreateContext())
        {
            Assert.Equal(3, await verify.CorporateEvents.CountAsync());
        }

        // Act — repete o mesmo conjunto
        var second = await RunImportAsync(Cash(0.55m), Cash(0.55m), Split());

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 3, 0, 0, 0), second);

        // Act — uma parcela some e entra um provento novo
        var third = await RunImportAsync(Cash(0.55m), Split(), Cash(0.70m));

        // Assert
        Assert.Equal(new CorporateEventImportResult(true, 1, 3, 0, 1, 1), third);
        await using var final = CreateContext();
        var amounts = await final.CorporateEvents
            .Where(e => e.Kind == CorporateEventKind.CashDividend)
            .Select(e => e.Amount)
            .OrderBy(a => a)
            .ToListAsync();
        Assert.Equal([0.55m, 0.70m], amounts);
    }

    [Fact(DisplayName = "Import de um emissor não mexe nos eventos de outro")]
    public async Task Import_DoesNotTouchOtherIssuers()
    {
        // Arrange
        await RunImportAsync(Cash(0.55m));
        await RunImportAsync("VALE", Cash(1.10m));

        // Act — reimporta PETR com conjunto vazio de eventos novos mas um diferente
        await RunImportAsync("PETR", Cash(0.60m));

        // Assert
        await using var verify = CreateContext();
        Assert.Equal(1, await verify.CorporateEvents.CountAsync(e => e.IssuerCode == "VALE"));
        Assert.Equal(0.60m, (await verify.CorporateEvents.SingleAsync(e => e.IssuerCode == "PETR")).Amount);
    }

    private static CorporateEventImportItem Cash(decimal amount) =>
        new(null, "ON", CorporateEventKind.CashDividend, CumDate, null, null, amount, 1, null, "DIVIDENDO");

    private static CorporateEventImportItem Split() =>
        new("BRPETRACNOR9", "ON", CorporateEventKind.StockSplit, new DateOnly(2008, 4, 25), null, null, null, null, 100m, "DESDOBRAMENTO");

    private static DailyQuote Quote(string ticker, string companyName, string isin, DateOnly tradingDate) =>
        DailyQuote.Create(
            Ticker.Create(ticker), tradingDate, QuoteMarketType.Cash, "02", companyName, "ON", isin,
            10m, 11m, 9m, 10m, 10m, 100, 1000, 10_000m, 1);

    private Task<CorporateEventImportResult> RunImportAsync(params CorporateEventImportItem[] items) =>
        RunImportAsync("PETR", items);

    private async Task<CorporateEventImportResult> RunImportAsync(string issuer, params CorporateEventImportItem[] items)
    {
        await using var context = CreateContext();
        var handler = new ImportCorporateEventsCommandHandler(
            new StubProvider(new CorporateEventFetch(items, 0)),
            new Repository<CorporateEvent>(context));

        return await handler.HandleAsync(new ImportCorporateEventsCommand(issuer, [issuer]));
    }

    private sealed class StubProvider(CorporateEventFetch fetch) : ICorporateEventProvider
    {
        public Task<CorporateEventFetch> GetAsync(
            string issuerCode,
            IReadOnlyCollection<string> tradingNames,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(fetch);
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
