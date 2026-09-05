using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class FuelReadsAveragePriceByBrandTests : IAsyncLifetime
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

    private static FuelStation CreateStation(string cnpj, string name, string brand, string state) =>
        FuelStation.Create(
            Cnpj.Create(cnpj),
            name,
            brand,
            Region.North,
            state,
            "CRUZEIRO DO SUL",
            null,
            null,
            null,
            null,
            null);

    [Fact(DisplayName =
        "AveragePricesByBrand calcula a média de preço por bandeira e estado, contra Postgres real")]
    public async Task AveragePricesByBrand_ComputesAveragePerBrandAndState_AgainstRealPostgres()
    {
        // Arrange: 2 postos IPIRANGA em SP (5,00 e 6,00 -> média 5,50), 1 posto SHELL em SP (4,00),
        // 1 posto IPIRANGA no RJ (10,00, não deve entrar no filtro por SP)
        await using (var seed = CreateContext())
        {
            var ipirangaSpA = CreateStation("01.492.748/0003-83", "Posto A", "IPIRANGA", "SP");
            var ipirangaSpB = CreateStation("11.222.333/0001-81", "Posto B", "IPIRANGA", "SP");
            var shellSp = CreateStation("22.333.444/0001-71", "Posto C", "SHELL", "SP");
            var ipirangaRj = CreateStation("33.444.555/0001-61", "Posto D", "IPIRANGA", "RJ");
            seed.FuelStations.AddRange(ipirangaSpA, ipirangaSpB, shellSp, ipirangaRj);
            await seed.SaveChangesAsync();

            seed.FuelPrices.AddRange(
                FuelPrice.Create(ipirangaSpA, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 5.00m, null, "R$ / litro"),
                FuelPrice.Create(ipirangaSpB, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 6.00m, null, "R$ / litro"),
                FuelPrice.Create(shellSp, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 4.00m, null, "R$ / litro"),
                FuelPrice.Create(ipirangaRj, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 10.00m, null, "R$ / litro"));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var readContext = CreateContext();
        var reads = new FuelReads(readContext);
        var result = await reads.AveragePricesByBrand(FuelProduct.Gasoline, "SP").ToListAsync();

        // Assert
        Assert.Equal(2, result.Count);

        var ipiranga = Assert.Single(result, r => r.Brand == "IPIRANGA");
        Assert.Equal("SP", ipiranga.State);
        Assert.Equal(5.50m, ipiranga.AveragePrice);
        Assert.Equal(2, ipiranga.StationCount);

        var shell = Assert.Single(result, r => r.Brand == "SHELL");
        Assert.Equal(4.00m, shell.AveragePrice);
        Assert.Equal(1, shell.StationCount);
    }

    [Fact(DisplayName = "AveragePricesByBrand sem estado devolve todas as combinações de bandeira e estado")]
    public async Task AveragePricesByBrand_WithoutState_ReturnsAllBrandStateCombinations()
    {
        // Arrange
        await using (var seed = CreateContext())
        {
            var ipirangaSp = CreateStation("01.492.748/0003-83", "Posto A", "IPIRANGA", "SP");
            var ipirangaRj = CreateStation("11.222.333/0001-81", "Posto B", "IPIRANGA", "RJ");
            seed.FuelStations.AddRange(ipirangaSp, ipirangaRj);
            await seed.SaveChangesAsync();

            seed.FuelPrices.AddRange(
                FuelPrice.Create(ipirangaSp, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 5.00m, null, "R$ / litro"),
                FuelPrice.Create(ipirangaRj, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 10.00m, null, "R$ / litro"));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var readContext = CreateContext();
        var reads = new FuelReads(readContext);
        var result = await reads.AveragePricesByBrand(FuelProduct.Gasoline).ToListAsync();

        // Assert: mesma bandeira, dois estados, duas linhas separadas — nunca uma média nacional
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Brand == "IPIRANGA" && r.State == "SP" && r.AveragePrice == 5.00m);
        Assert.Contains(result, r => r.Brand == "IPIRANGA" && r.State == "RJ" && r.AveragePrice == 10.00m);
    }
}
