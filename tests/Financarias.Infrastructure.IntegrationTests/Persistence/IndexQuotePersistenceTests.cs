using Financarias.Domain.MarketData.Indexes;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class IndexQuotePersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly TradingDate = new(2018, 10, 8);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Persiste e relê a cotação do índice com o fechamento em duas casas")]
    public async Task SaveAndRead_RoundTripsIndexQuote()
    {
        // Arrange
        long id;
        await using (var write = CreateContext())
        {
            var quote = Quote(TradingDate, 86_083.92m);
            write.IndexQuotes.Add(quote);
            await write.SaveChangesAsync();
            id = quote.Id;
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.IndexQuotes.SingleAsync(q => q.Id == id);

        // Assert
        Assert.True(loaded.Id > 0);
        Assert.Equal(MarketIndex.Ibovespa, loaded.MarketIndex);
        Assert.Equal(TradingDate, loaded.TradingDate);
        Assert.Equal(86_083.92m, loaded.CloseValue);
        Assert.Equal(IndexQuoteSource.B3, loaded.Source);
    }

    [Fact(DisplayName = "Índice único barra duas cotações do mesmo índice no mesmo pregão")]
    public async Task UniqueIndex_RejectsDuplicateQuote()
    {
        // Arrange
        await using var context = CreateContext();
        context.IndexQuotes.Add(Quote(TradingDate, 86_083.92m));
        await context.SaveChangesAsync();

        // Act
        context.IndexQuotes.Add(Quote(TradingDate, 86_000.00m));

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(DisplayName = "Pregões diferentes do mesmo índice coexistem")]
    public async Task UniqueIndex_AllowsDifferentTradingDays()
    {
        // Arrange
        await using var context = CreateContext();
        context.IndexQuotes.Add(Quote(TradingDate, 86_083.92m));
        context.IndexQuotes.Add(Quote(TradingDate.AddDays(1), 86_087.50m));

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.IndexQuotes.CountAsync());
    }

    [Fact(DisplayName = "CHECK do banco barra fechamento não positivo inserido fora do agregado")]
    public async Task CheckConstraint_RejectsNonPositiveClose_InsertedBypassingTheAggregate()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO index_quotes (market_index, trading_date, close_value, source)
                VALUES ('Ibovespa', {TradingDate}, 0, 'B3')
                """));

        // Assert
        Assert.Equal("ck_index_quotes_close_value_positive", exception.ConstraintName);
    }

    private static IndexQuote Quote(DateOnly tradingDate, decimal closeValue) =>
        IndexQuote.Create(MarketIndex.Ibovespa, tradingDate, closeValue, IndexQuoteSource.B3);

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }
}
