using Financarias.Domain.MarketData.Indicators;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class IndicatorValuePersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly ReferenceDate = new(2026, 10, 9);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Persiste e relê o valor do indicador com quatro casas decimais")]
    public async Task SaveAndRead_RoundTripsValue()
    {
        // Arrange
        long id;
        await using (var write = CreateContext())
        {
            var value = IndicatorValue.Create(EconomicIndicator.Dollar, ReferenceDate, 4.9892m);
            write.IndicatorValues.Add(value);
            await write.SaveChangesAsync();
            id = value.Id;
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.IndicatorValues.SingleAsync(v => v.Id == id);

        // Assert
        Assert.True(loaded.Id > 0);
        Assert.Equal(EconomicIndicator.Dollar, loaded.Indicator);
        Assert.Equal(ReferenceDate, loaded.ReferenceDate);
        Assert.Equal(4.9892m, loaded.Value);
    }

    [Fact(DisplayName = "Persiste valor negativo (deflação do IPCA)")]
    public async Task SaveAndRead_RoundTripsNegativeValue()
    {
        // Arrange
        await using (var write = CreateContext())
        {
            write.IndicatorValues.Add(IndicatorValue.Create(EconomicIndicator.Ipca, new DateOnly(2026, 8, 1), -0.32m));
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.IndicatorValues.SingleAsync();

        // Assert
        Assert.Equal(-0.32m, loaded.Value);
    }

    [Fact(DisplayName = "Índice único barra o mesmo indicador na mesma data")]
    public async Task UniqueIndex_RejectsDuplicateValue()
    {
        // Arrange
        await using var context = CreateContext();
        context.IndicatorValues.Add(IndicatorValue.Create(EconomicIndicator.Cdi, ReferenceDate, 13.65m));
        await context.SaveChangesAsync();

        // Act
        context.IndicatorValues.Add(IndicatorValue.Create(EconomicIndicator.Cdi, ReferenceDate, 13.60m));

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(DisplayName = "Indicadores diferentes na mesma data coexistem")]
    public async Task UniqueIndex_AllowsDifferentIndicatorsOnTheSameDate()
    {
        // Arrange
        await using var context = CreateContext();
        context.IndicatorValues.Add(IndicatorValue.Create(EconomicIndicator.Cdi, ReferenceDate, 13.65m));
        context.IndicatorValues.Add(IndicatorValue.Create(EconomicIndicator.SelicTarget, ReferenceDate, 13.75m));

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.IndicatorValues.CountAsync());
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
