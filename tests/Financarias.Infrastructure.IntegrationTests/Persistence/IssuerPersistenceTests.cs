using Financarias.Application.MarketData.CorporateEvents.Queries;
using Financarias.Application.MarketData.Issuers.Commands;
using Financarias.Application.MarketData.Issuers.Import;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Issuers;
using Financarias.Domain.MarketData.Stocks;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class IssuerPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Persiste e relê o emissor com CNPJ, classificação setorial e atividade longa")]
    public async Task SaveAndRead_RoundTripsTheIssuer()
    {
        // Arrange
        var activity = new string('x', 2_000);
        await using (var write = CreateContext())
        {
            write.Issuers.Add(Issuer.Create(
                "PETR", "9512", "PETROLEO BRASILEIRO S.A. PETROBRAS", "PETROBRAS", Cnpj.Create("33000167000101"),
                "Petróleo. Gás e Biocombustíveis", "Petróleo. Gás e Biocombustíveis", "Exploração. Refino e Distribuição",
                "BOVESPA NIVEL 2", activity, "www.petrobras.com.br"));
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.Issuers.SingleAsync();

        // Assert
        Assert.Equal("33000167000101", loaded.Cnpj?.Value);
        Assert.Equal("Exploração. Refino e Distribuição", loaded.Segment);
        Assert.Equal(activity, loaded.Activity);
    }

    [Fact(DisplayName = "Índice único barra dois emissores com o mesmo código")]
    public async Task UniqueIndex_RejectsTheSameIssuerCodeTwice()
    {
        // Arrange
        await using var context = CreateContext();
        context.Issuers.Add(Issuer.Create("PETR", "9512", "A", "A", null, null, null, null, null, null, null));
        await context.SaveChangesAsync();

        await using var other = CreateContext();
        other.Issuers.Add(Issuer.Create("PETR", "9999", "B", "B", null, null, null, null, null, null, null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());
        Assert.Equal("23505", ((PostgresException)exception.InnerException!).SqlState);
    }

    [Fact(DisplayName = "IssuerCodesAsync lista os emissores distintos pelo prefixo do ISIN das cotações, em ordem")]
    public async Task IssuerCodes_ListsTheDistinctIssuersFromTheQuoteIsins()
    {
        // Arrange
        await using (var write = CreateContext())
        {
            write.DailyQuotes.AddRange(
                Quote("PETR4", "BRPETRACNPR6", new DateOnly(2026, 10, 8)),
                Quote("PETR4", "BRPETRACNPR6", new DateOnly(2026, 10, 9)),
                Quote("PETR3", "BRPETRACNOR9", new DateOnly(2026, 10, 9)),
                Quote("VALE3", "BRVALEACNOR0", new DateOnly(2026, 10, 9)));
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var codes = await new IssuerReads(read).IssuerCodesAsync();

        // Assert
        Assert.Equal(["PETR", "VALE"], codes);
    }

    [Fact(DisplayName = "O handler cria na primeira vez e atualiza na segunda, só o que mudou")]
    public async Task Handler_CreatesThenUpdatesOnlyWhatChanged()
    {
        // Arrange
        var first = new StubProvider(Item("Antigo"));
        var second = new StubProvider(Item("Petróleo"));

        // Act
        IssuerImportResult created;
        IssuerImportResult updated;
        IssuerImportResult unchanged;

        await using (var context = CreateContext())
        {
            created = await new ImportIssuersCommandHandler(first, new Repository<Issuer>(context))
                .HandleAsync(new ImportIssuersCommand(["PETR"]));
        }

        await using (var context = CreateContext())
        {
            updated = await new ImportIssuersCommandHandler(second, new Repository<Issuer>(context))
                .HandleAsync(new ImportIssuersCommand(["PETR"]));
        }

        await using (var context = CreateContext())
        {
            unchanged = await new ImportIssuersCommandHandler(second, new Repository<Issuer>(context))
                .HandleAsync(new ImportIssuersCommand(["PETR"]));
        }

        // Assert
        Assert.Equal(new IssuerImportResult(1, 0, 1, 0), created);
        Assert.Equal(new IssuerImportResult(1, 0, 0, 1), updated);
        Assert.Equal(new IssuerImportResult(1, 0, 0, 0), unchanged);
        await using var read = CreateContext();
        Assert.Equal("Petróleo", (await read.Issuers.SingleAsync()).Sector);
    }

    private static IssuerImportItem Item(string sector) =>
        new("PETR", "9512", "PETROLEO BRASILEIRO S.A. PETROBRAS", "PETROBRAS", "33000167000101", sector, null, null, null, null, null);

    private static DailyQuote Quote(string ticker, string isin, DateOnly day) =>
        DailyQuote.Create(
            Ticker.Create(ticker), day, QuoteMarketType.Cash, "02", "PETROBRAS", "PN", isin,
            10m, 11m, 9m, 10m, 10m, 100, 1000, 10_000m, 1);

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }

    private sealed class StubProvider(IssuerImportItem item) : IIssuerProvider
    {
        public Task<IssuerFetch> GetAsync(IReadOnlyCollection<string> issuerCodes, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IssuerFetch([item], 0));
    }
}
