using Financarias.Application.MarketData.Stocks.Specifications;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class DailyQuotePersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly TradingDate = new(2026, 10, 9);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Persiste e relê a cotação (round-trip com Ticker, mercado e valores grandes)")]
    public async Task SaveAndRead_RoundTripsQuote()
    {
        // Arrange
        long id;
        await using (var write = CreateContext())
        {
            var quote = Quote(
                "PETR4",
                trades: 92_549,
                quantity: 92_693_400,
                volume: 2_793_039_893.00m,
                quoteFactor: 100);
            write.DailyQuotes.Add(quote);
            await write.SaveChangesAsync();
            id = quote.Id;
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.DailyQuotes.SingleAsync(q => q.Id == id);

        // Assert
        Assert.True(loaded.Id > 0);
        Assert.Equal("PETR4", loaded.Ticker.Value);
        Assert.Equal(TradingDate, loaded.TradingDate);
        Assert.Equal(QuoteMarketType.Cash, loaded.MarketType);
        Assert.Equal("02", loaded.BdiCode);
        Assert.Equal("PETROBRAS", loaded.CompanyName);
        Assert.Equal("PN      N2", loaded.Specification);
        Assert.Equal("BRPETRACNPR6", loaded.Isin);
        Assert.Equal(38.10m, loaded.OpenPrice);
        Assert.Equal(38.90m, loaded.HighPrice);
        Assert.Equal(37.80m, loaded.LowPrice);
        Assert.Equal(38.40m, loaded.AveragePrice);
        Assert.Equal(38.70m, loaded.ClosePrice);
        Assert.Equal(92_549, loaded.Trades);
        Assert.Equal(92_693_400, loaded.Quantity);
        Assert.Equal(2_793_039_893.00m, loaded.Volume);
        Assert.Equal(100, loaded.QuoteFactor);
    }

    [Fact(DisplayName = "Índice único barra cotação duplicada (papel + pregão + mercado)")]
    public async Task UniqueIndex_RejectsDuplicateQuote()
    {
        // Arrange
        await using var context = CreateContext();
        context.DailyQuotes.Add(Quote("PETR4"));
        await context.SaveChangesAsync();

        // Act
        context.DailyQuotes.Add(Quote("PETR4"));

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(DisplayName = "Mesmo papel e pregão em mercados diferentes (vista e fracionário) coexistem")]
    public async Task UniqueIndex_AllowsSameTickerAndDay_InDifferentMarkets()
    {
        // Arrange
        await using var context = CreateContext();
        context.DailyQuotes.Add(Quote("PETR4", QuoteMarketType.Cash));
        context.DailyQuotes.Add(Quote("PETR4", QuoteMarketType.Fractional));

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.DailyQuotes.CountAsync());
    }

    [Theory(DisplayName = "CHECK do banco barra linhas inválidas que contornam o agregado")]
    [InlineData(0, 38.90, 37.80, 100, 1, "ck_daily_quotes_prices_positive")]
    [InlineData(38.10, 37.00, 37.80, 100, 1, "ck_daily_quotes_high_not_below_low")]
    [InlineData(38.10, 38.90, 37.80, 0, 1, "ck_daily_quotes_counts_positive")]
    [InlineData(38.10, 38.90, 37.80, 100, 0, "ck_daily_quotes_counts_positive")]
    public async Task CheckConstraints_RejectInvalidRows_InsertedBypassingTheAggregate(
        decimal open, decimal high, decimal low, long quantity, int quoteFactor, string constraint)
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO daily_quotes
                    (ticker, trading_date, market_type, bdi_code, company_name, specification, isin,
                     open_price, high_price, low_price, average_price, close_price,
                     trades, quantity, volume, quote_factor)
                VALUES
                    ('PETR4', '2026-10-09', 'Cash', '02', 'PETROBRAS', 'PN', 'BRPETRACNPR6',
                     {open}, {high}, {low}, 38.4, 38.7,
                     10, {quantity}, 1000.5, {quoteFactor})
                """));

        // Assert
        Assert.Equal(constraint, exception.ConstraintName);
    }

    [Fact(DisplayName = "Specification por papéis e datas traduz o Contains do VO Ticker para SQL")]
    public async Task Specification_FiltersByTickersAndDates_ThroughTheTickerConverter()
    {
        // Arrange
        await using (var write = CreateContext())
        {
            write.DailyQuotes.AddRange(
                Quote("PETR4"),
                Quote("VALE3"),
                Quote("ITUB4"),
                Quote("PETR4", tradingDate: TradingDate.AddDays(-1)));
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var repository = new Repository<DailyQuote>(read);

        // Act
        var found = await repository.ListAsync(new QuotesByTickersAndDatesSpecification(
            [Ticker.Create("PETR4"), Ticker.Create("VALE3")],
            [TradingDate]));

        // Assert
        Assert.Equal(["PETR4", "VALE3"], found.Select(q => q.Ticker.Value).Order().ToArray());
    }

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }

    private static DailyQuote Quote(
        string ticker,
        QuoteMarketType marketType = QuoteMarketType.Cash,
        DateOnly? tradingDate = null,
        int trades = 52_310,
        long quantity = 41_250_300,
        decimal volume = 1_583_000_000.55m,
        int quoteFactor = 1) =>
        DailyQuote.Create(
            Ticker.Create(ticker),
            tradingDate ?? TradingDate,
            marketType,
            "02",
            "PETROBRAS",
            "PN      N2",
            "BRPETRACNPR6",
            38.10m,
            38.90m,
            37.80m,
            38.40m,
            38.70m,
            trades,
            quantity,
            volume,
            quoteFactor);
}
