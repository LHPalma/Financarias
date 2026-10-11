using Financarias.Domain.MarketData.Inflation;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class InflationLevelPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Persiste e relê o nível diário com treze algarismos inteiros e dezesseis casas")]
    public async Task SaveAndRead_RoundTripsALargeLevel()
    {
        // Arrange
        var level = 1_010_548_936_213.1234567890123456m;
        await using (var write = CreateContext())
        {
            write.InflationLevels.Add(InflationLevel.Create(new DateOnly(2026, 10, 15), level));
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.InflationLevels.SingleAsync();

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 15), loaded.Day);
        Assert.Equal(level, loaded.Level);
    }

    [Fact(DisplayName = "Índice único barra dois níveis para o mesmo dia")]
    public async Task UniqueIndex_RejectsTheSameDayTwice()
    {
        // Arrange
        await using var context = CreateContext();
        context.InflationLevels.Add(InflationLevel.Create(new DateOnly(2026, 10, 15), 1m));
        await context.SaveChangesAsync();

        // Act
        context.InflationLevels.Add(InflationLevel.Create(new DateOnly(2026, 10, 15), 2m));

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(DisplayName = "CHECK do banco barra nível não positivo inserido fora do agregado")]
    public async Task CheckConstraint_RejectsNonPositiveLevel_InsertedBypassingTheAggregate()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync("INSERT INTO inflation_levels (day, level) VALUES ('2026-10-15', 0)"));

        // Assert
        Assert.Equal("ck_inflation_levels_level_positive", exception.ConstraintName);
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
