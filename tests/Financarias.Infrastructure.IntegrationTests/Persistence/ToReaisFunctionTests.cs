using Financarias.Domain.MarketData.Currencies;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class ToReaisFunctionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Theory(DisplayName = "A função to_reais do Postgres dá o mesmo resultado do conversor do domínio, nos dois lados de cada troca")]
    [InlineData("2026-10-09", "4.9892")]
    [InlineData("1994-07-01", "1.0")]
    [InlineData("1994-06-30", "2750.0")]
    [InlineData("1993-08-01", "72.069")]
    [InlineData("1993-07-31", "71153.0")]
    [InlineData("1990-03-16", "400.00")]
    [InlineData("1990-03-15", "30.00")]
    [InlineData("1989-01-16", "1.0")]
    [InlineData("1989-01-15", "859.25")]
    [InlineData("1986-02-28", "13.84")]
    [InlineData("1986-02-27", "13750.0")]
    [InlineData("1984-11-28", "2828.0")]
    [InlineData("1970-05-15", "6.07")]
    public async Task ToReais_MatchesTheDomainConverter(string date, string amount)
    {
        // Arrange
        var day = DateOnly.Parse(date);
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        await using var context = CreateContext();

        // Act
        var inDatabase = await context.Database
            .SqlQuery<decimal>($"SELECT to_reais({value}::numeric, {day}::date) AS \"Value\"")
            .SingleAsync();
        var inDomain = BrazilianCurrency.ToReais(value, day);

        // Assert: o Postgres divide com 16 algarismos significativos ou mais; o decimal tem 28
        var tolerance = Math.Abs(inDomain) * 1e-12m;
        Assert.InRange(inDatabase, inDomain - tolerance, inDomain + tolerance);
    }

    [Fact(DisplayName = "A função to_reais recusa data anterior à primeira suportada, como o domínio")]
    public async Task ToReais_RejectsDatesBeforeTheFirstSupportedOne()
    {
        // Arrange
        await using var context = CreateContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync("SELECT to_reais(1::numeric, DATE '1970-05-14')"));

        Assert.Equal("22008", exception.SqlState);
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
