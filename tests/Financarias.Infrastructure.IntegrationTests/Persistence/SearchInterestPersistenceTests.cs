using Financarias.Application.MarketData.SearchInterest.Commands;
using Financarias.Application.MarketData.SearchInterest.Import;
using Financarias.Domain.MarketData.SearchInterest;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class SearchInterestPersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly From = new(2023, 1, 1);
    private static readonly DateOnly To = new(2023, 1, 31);
    private static readonly string[] Terms = ["americanas", "magalu"];

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Persiste e relê o ponto com o grupo, o intervalo e o valor")]
    public async Task SaveAndRead_RoundTripsAPoint()
    {
        // Arrange
        await using (var write = CreateContext())
        {
            write.SearchInterestPoints.Add(SearchInterestPoint.Create("BR", Terms, From, To, "americanas", new DateOnly(2023, 1, 12), 100));
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.SearchInterestPoints.SingleAsync();

        // Assert
        Assert.Equal("americanas|magalu", loaded.ComparisonKey);
        Assert.Equal(From, loaded.RangeStart);
        Assert.Equal(100, loaded.Value);
    }

    [Fact(DisplayName = "O mesmo termo e dia em outro intervalo é outro ponto, porque a escala do Google é outra")]
    public async Task SameTermAndDay_InAnotherRange_IsAnotherPoint()
    {
        // Arrange
        await using var context = CreateContext();
        context.SearchInterestPoints.AddRange(
            SearchInterestPoint.Create("BR", Terms, From, To, "americanas", new DateOnly(2023, 1, 12), 100),
            SearchInterestPoint.Create("BR", Terms, From, To.AddDays(30), "americanas", new DateOnly(2023, 1, 12), 71));

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.SearchInterestPoints.CountAsync());
    }

    [Fact(DisplayName = "Índice único barra dois pontos do mesmo grupo, intervalo, termo e dia")]
    public async Task UniqueIndex_RejectsTheSamePointTwice()
    {
        // Arrange
        await using (var first = CreateContext())
        {
            first.SearchInterestPoints.Add(SearchInterestPoint.Create("BR", Terms, From, To, "americanas", new DateOnly(2023, 1, 12), 100));
            await first.SaveChangesAsync();
        }

        await using var second = CreateContext();
        second.SearchInterestPoints.Add(SearchInterestPoint.Create("BR", Terms, From, To, "americanas", new DateOnly(2023, 1, 12), 50));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
        Assert.Equal("23505", ((PostgresException)exception.InnerException!).SqlState);
    }

    [Fact(DisplayName = "O CHECK do banco recusa valor fora de 0 a 100 mesmo por SQL direto")]
    public async Task Check_RejectsAValueOutOfRange()
    {
        // Arrange
        await using var context = CreateContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO search_interest_points (geo, comparison_key, range_start, range_end, term, day, value) " +
            "VALUES ('BR', 'a', '2023-01-01', '2023-01-31', 'a', '2023-01-02', 101)"));

        Assert.Equal("23514", exception.SqlState);
    }

    [Fact(DisplayName = "O handler cria na primeira vez, atualiza só o que mudou na segunda e não grava nada na terceira")]
    public async Task Handler_CreatesThenUpdatesOnlyWhatChanged()
    {
        // Arrange
        var first = new StubProvider(Item("americanas", 12, 100), Item("magalu", 12, 9));
        var second = new StubProvider(Item("americanas", 12, 98), Item("magalu", 12, 9));
        var command = new ImportSearchInterestCommand(Terms, "BR", From, To);

        // Act
        var created = await RunAsync(first, command);
        var updated = await RunAsync(second, command);
        var unchanged = await RunAsync(second, command);

        // Assert
        Assert.Equal(new SearchInterestImportResult(2, 2, 0), created);
        Assert.Equal(new SearchInterestImportResult(2, 0, 1), updated);
        Assert.Equal(new SearchInterestImportResult(2, 0, 0), unchanged);
        await using var read = CreateContext();
        Assert.Equal(98, (await read.SearchInterestPoints.SingleAsync(p => p.Term == "americanas")).Value);
    }

    private async Task<SearchInterestImportResult> RunAsync(ISearchInterestProvider provider, ImportSearchInterestCommand command)
    {
        await using var context = CreateContext();

        return await new ImportSearchInterestCommandHandler(provider, new Repository<SearchInterestPoint>(context)).HandleAsync(command);
    }

    private static SearchInterestImportItem Item(string term, int day, int value) => new(term, new DateOnly(2023, 1, day), value);

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }

    private sealed class StubProvider(params SearchInterestImportItem[] items) : ISearchInterestProvider
    {
        public Task<IReadOnlyList<SearchInterestImportItem>> GetAsync(
            IReadOnlyList<string> terms,
            string geo,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SearchInterestImportItem>>(items);
    }
}
