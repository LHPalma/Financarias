using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class FuelReadsTests : IAsyncLifetime
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

    private static FuelStation CreateStation(string cnpj, string name) =>
        FuelStation.Create(
            Cnpj.Create(cnpj),
            name,
            "IPIRANGA",
            Region.North,
            "AC",
            "CRUZEIRO DO SUL",
            null,
            null,
            null,
            null,
            null);

    [Fact(DisplayName =
        "LatestPricesByProduct devolve a coleta mais recente de cada posto, sem misturar postos nem produtos")]
    public async Task LatestPricesByProduct_ReturnsMostRecentPerStation_AgainstRealPostgres()
    {
        // Arrange: posto A tem duas coletas de etanol (uma antiga, uma nova) + uma de gasolina;
        // posto B tem só uma coleta de etanol, mais antiga que a mais recente de A
        await using (var seed = CreateContext())
        {
            var stationA = CreateStation("01.492.748/0003-83", "Posto Copacabana");
            var stationB = CreateStation("11.222.333/0001-81", "Posto Ipanema");
            seed.FuelStations.AddRange(stationA, stationB);
            await seed.SaveChangesAsync();

            seed.FuelPrices.AddRange(
                FuelPrice.Create(stationA, FuelProduct.Ethanol, new DateOnly(2026, 7, 1), 3.80m, null, "R$ / litro"),
                FuelPrice.Create(stationA, FuelProduct.Ethanol, new DateOnly(2026, 8, 1), 3.50m, null, "R$ / litro"),
                FuelPrice.Create(stationA, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 5.00m, null, "R$ / litro"),
                FuelPrice.Create(stationB, FuelProduct.Ethanol, new DateOnly(2026, 6, 1), 3.90m, null, "R$ / litro"));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var readContext = CreateContext();
        var reads = new FuelReads(readContext);
        var result = await reads.LatestPricesByProduct(FuelProduct.Ethanol)
            .Include(p => p.FuelStation)
            .ToListAsync();

        // Assert
        Assert.Equal(2, result.Count);

        var stationAPrice = Assert.Single(result, p => p.FuelStation.Name == "Posto Copacabana");
        Assert.Equal(3.50m, stationAPrice.SalePrice); // a mais recente (agosto), não a de julho

        var stationBPrice = Assert.Single(result, p => p.FuelStation.Name == "Posto Ipanema");
        Assert.Equal(3.90m, stationBPrice.SalePrice);
    }
}
