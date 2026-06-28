using Financarias.Domain.Holidays;
using Financarias.Domain.Holidays.Models;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class HolidayPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }

    [Fact(DisplayName = "Persiste e relê um feriado (round-trip contra Postgres real)")]
    public async Task SaveAndRead_RoundTripsHoliday()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 1);
        await using (var write = CreateContext())
        {
            write.Holidays.Add(Holiday.Create(date, "Confraternização Universal", CountryCode.BR));
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var holiday = await read.Holidays.SingleAsync(h => h.Date == date && h.CountryCode == CountryCode.BR);

        // Assert
        Assert.True(holiday.Id > 0); // gerado pelo banco (identity)
        Assert.Equal(date, holiday.Date);
        Assert.Equal("Confraternização Universal", holiday.Name);
        Assert.Equal(CountryCode.BR, holiday.CountryCode);
    }

    [Fact(DisplayName = "Índice único barra feriado duplicado (mesma data + país)")]
    public async Task UniqueIndex_RejectsDuplicate()
    {
        // Arrange
        var date = new DateOnly(2024, 9, 7);
        await using var context = CreateContext();
        context.Holidays.Add(Holiday.Create(date, "Independência", CountryCode.BR));
        await context.SaveChangesAsync();

        // Act
        context.Holidays.Add(Holiday.Create(date, "Duplicado", CountryCode.BR));

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}