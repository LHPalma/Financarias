using Financarias.Application.MarketData.Inflation.Commands;
using Financarias.Domain.Holidays.Models;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Domain.MarketData.Inflation;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class RebuildInflationLevelsHandlerTests : IAsyncLifetime
{
    private static readonly DateOnly[] Holidays =
    [
        new(1994, 4, 21),
        new(1994, 9, 7),
        new(1994, 10, 12),
        new(1994, 11, 2),
        new(1994, 11, 15),
        new(1994, 12, 25),
        new(1995, 1, 2),
    ];

    private static readonly (DateOnly Month, decimal Rate)[] Ipca =
    [
        (new DateOnly(1994, 7, 1), 6.84m),
        (new DateOnly(1994, 8, 1), 1.86m),
        (new DateOnly(1994, 9, 1), 1.53m),
        (new DateOnly(1994, 10, 1), 2.62m),
        (new DateOnly(1994, 11, 1), 2.81m),
        (new DateOnly(1994, 12, 1), 1.70m),
    ];

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedAsync(context);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Grava um nível por dia corrido e uma segunda execução não altera nada")]
    public async Task Rebuild_PersistsEveryCalendarDay_AndIsIdempotent()
    {
        // Arrange
        InflationLevelsRebuildResult first;
        InflationLevelsRebuildResult second;

        // Act
        await using (var context = CreateContext())
        {
            first = await CreateHandler(context).HandleAsync(new RebuildInflationLevelsCommand());
        }

        await using (var context = CreateContext())
        {
            second = await CreateHandler(context).HandleAsync(new RebuildInflationLevelsCommand());
        }

        // Assert
        var days = first.LastDay.DayNumber - first.FirstDay.DayNumber + 1;
        Assert.Equal(days, first.Days);
        Assert.Equal(days, first.LevelsCreated);
        Assert.Equal(new InflationLevelsRebuildResult(first.FirstDay, first.LastDay, days, 0, 0, 0), second);
        await using var read = CreateContext();
        Assert.Equal(days, await read.InflationLevels.CountAsync());
    }

    [Theory(DisplayName = "to_present_value do Postgres dá o mesmo resultado do índice do domínio, inclusive com moeda antiga")]
    [InlineData("1994-07-15", "2750.0", "1994-12-15")]
    [InlineData("1994-08-20", "100.0", "1995-01-16")]
    [InlineData("1994-12-15", "100.0", "1994-12-15")]
    [InlineData("1994-07-18", "100.0", "1994-11-30")]
    public async Task ToPresentValue_MatchesTheDomainIndex(string date, string amount, string baseDate)
    {
        // Arrange
        await RebuildAsync();
        var day = DateOnly.Parse(date);
        var baseDay = DateOnly.Parse(baseDate);
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        var index = InflationIndex.FromMonthlyRates(
            Ipca.Select(item => (item.Month, item.Rate)),
            Holidays.ToHashSet());

        // Act
        await using var context = CreateContext();
        var inDatabase = await context.Database
            .SqlQuery<decimal>($"SELECT to_present_value({value}::numeric, {day}::date, {baseDay}::date) AS \"Value\"")
            .SingleAsync();
        var inDomain = Financarias.Domain.MarketData.Currencies.BrazilianCurrency.ToReais(value, day)
            * index.Level(baseDay) / index.Level(day);

        // Assert
        var tolerance = (Math.Abs(inDomain) * 1e-12m) + 1e-8m;
        Assert.InRange(inDatabase, inDomain - tolerance, inDomain + tolerance);
    }

    [Fact(DisplayName = "to_present_value sem data-base usa o último dia do índice")]
    public async Task ToPresentValue_WithoutBaseDay_UsesTheLatestLevel()
    {
        // Arrange
        var result = await RebuildAsync();
        await using var context = CreateContext();

        // Act
        var implicitBase = await context.Database
            .SqlQuery<decimal>($"SELECT to_present_value(100::numeric, DATE '1994-08-20') AS \"Value\"")
            .SingleAsync();
        var explicitBase = await context.Database
            .SqlQuery<decimal>($"SELECT to_present_value(100::numeric, DATE '1994-08-20', {result.LastDay}::date) AS \"Value\"")
            .SingleAsync();

        // Assert
        Assert.Equal(explicitBase, implicitBase);
    }

    [Fact(DisplayName = "to_present_value recusa dia fora do índice")]
    public async Task ToPresentValue_RejectsADayOutsideTheIndex()
    {
        // Arrange
        await RebuildAsync();
        await using var context = CreateContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync("SELECT to_present_value(1::numeric, DATE '1994-07-14')"));

        Assert.Equal("22008", exception.SqlState);
    }

    private async Task<InflationLevelsRebuildResult> RebuildAsync()
    {
        await using var context = CreateContext();

        return await CreateHandler(context).HandleAsync(new RebuildInflationLevelsCommand());
    }

    private static RebuildInflationLevelsCommandHandler CreateHandler(FinancariasDbContext context) =>
        new(
            new Repository<IndicatorValue>(context),
            new Repository<Holiday>(context),
            new Repository<InflationLevel>(context));

    private static async Task SeedAsync(FinancariasDbContext context)
    {
        context.IndicatorValues.AddRange(
            Ipca.Select(item => IndicatorValue.Create(EconomicIndicator.Ipca, item.Month, item.Rate)));
        context.Holidays.AddRange(
            Holidays.Select(date => Holiday.Create(date, "Feriado", CountryCode.BR)));
        await context.SaveChangesAsync();
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
