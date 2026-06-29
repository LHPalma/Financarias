using Financarias.Application.Analytics.Queries;
using Financarias.Domain.Analytics;
using Financarias.Domain.Holidays.Models;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class CalculateNtnbPriceQueryHandlerTests : IAsyncLifetime
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

    [Fact(DisplayName = "Carrega os feriados BR do banco e precifica com T+2 sobre o feriado")]
    public async Task Handle_LoadsHolidaysFromDatabaseAndPrices()
    {
        // Arrange — feriado em 03/01 no banco empurra o T+2 de 04 para 05/01
        await using (var seed = CreateContext())
        {
            var repository = new Repository<Holiday>(seed);
            await repository.AddAsync(Holiday.Create(new DateOnly(2024, 1, 3), "Feriado teste", CountryCode.BR));
        }

        var query = new CalculateNtnbPriceQuery(
            NominalValue.Create(1000m),
            AnnualYield.FromFraction(0.06m),
            0.0046m,
            new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 15));

        await using var context = CreateContext();
        var handler = new CalculateNtnbPriceQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert — sem o feriado a liquidação seria 04/01; com ele, 05/01 (prova que veio do banco)
        Assert.Equal(new DateOnly(2024, 1, 5), result.SettlementDate);
        Assert.Equal(6, result.BusinessDaysToMaturity);
        Assert.True(result.UnitPrice > 0m);
    }
}
