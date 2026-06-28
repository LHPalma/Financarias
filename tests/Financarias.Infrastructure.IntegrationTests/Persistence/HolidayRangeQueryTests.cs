using Financarias.Application.Holidays.Queries;
using Financarias.Domain.Holidays.Models;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class HolidayRangeQueryTests : IAsyncLifetime
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

    [Fact(DisplayName = "Semeia pelo IRepository<Holiday> e relê só os feriados dentro do range e do país")]
    public async Task FindInRange_ReturnsOnlyHolidaysWithinRangeAndCountry()
    {
        // Arrange — escrita via repositório Ardalis (AddRangeAsync já persiste)
        await using (var seed = CreateContext())
        {
            var repository = new Repository<Holiday>(seed);
            await repository.AddRangeAsync(
            [
                Holiday.Create(new DateOnly(2024, 12, 25), "Natal", CountryCode.BR),               // dentro
                Holiday.Create(new DateOnly(2025, 1, 1), "Confraternização", CountryCode.BR),       // dentro (borda final)
                Holiday.Create(new DateOnly(2025, 2, 1), "Fora do range", CountryCode.BR),          // fora do range
            ]);
        }

        // Act
        await using var read = CreateContext();
        var handler = new FindHolidaysInRangeQueryHandler(read);
        var holidays = await handler.HandleAsync(
            new FindHolidaysInRangeQuery(new DateOnly(2024, 12, 1), new DateOnly(2025, 1, 1), CountryCode.BR));

        // Assert
        Assert.Equal(2, holidays.Count);
        Assert.Contains(new DateOnly(2024, 12, 25), holidays);
        Assert.Contains(new DateOnly(2025, 1, 1), holidays);
        Assert.DoesNotContain(new DateOnly(2025, 2, 1), holidays);
    }
}
