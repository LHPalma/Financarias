using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class FuelReadsRankingTests : IAsyncLifetime
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

    private static FuelStation CreateStation(string cnpj, string name, string state, string municipality) =>
        FuelStation.Create(
            Cnpj.Create(cnpj),
            name,
            "IPIRANGA",
            Region.North,
            state,
            municipality,
            null,
            null,
            null,
            null,
            null);

    [Fact(DisplayName = "AveragePriceByState calcula a média de preço por estado, sem misturar estados")]
    public async Task AveragePriceByState_ComputesAveragePerState_AgainstRealPostgres()
    {
        // Arrange: 2 postos em SP (5,00 e 6,00 -> média 5,50), 1 posto no RJ (10,00)
        await using (var seed = CreateContext())
        {
            var spA = CreateStation("01.492.748/0003-83", "Posto A", "SP", "SAO PAULO");
            var spB = CreateStation("11.222.333/0001-81", "Posto B", "SP", "CAMPINAS");
            var rj = CreateStation("22.333.444/0001-71", "Posto C", "RJ", "NITEROI");
            seed.FuelStations.AddRange(spA, spB, rj);
            await seed.SaveChangesAsync();

            seed.FuelPrices.AddRange(
                FuelPrice.Create(spA, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 5.00m, null, "R$ / litro"),
                FuelPrice.Create(spB, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 6.00m, null, "R$ / litro"),
                FuelPrice.Create(rj, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 10.00m, null, "R$ / litro"));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var readContext = CreateContext();
        var reads = new FuelReads(readContext);
        var result = (await reads.AveragePriceByState(FuelProduct.Gasoline)).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        var sp = Assert.Single(result, r => r.State == "SP");
        Assert.Equal(5.50m, sp.AveragePrice);
        Assert.Equal(2, sp.StationCount);

        var rjResult = Assert.Single(result, r => r.State == "RJ");
        Assert.Equal(10.00m, rjResult.AveragePrice);
        Assert.Equal(1, rjResult.StationCount);
    }

    [Fact(DisplayName =
        "AveragePriceByMunicipality não mistura municípios homônimos de estados diferentes")]
    public async Task AveragePriceByMunicipality_DisambiguatesHomonymMunicipalities_AgainstRealPostgres()
    {
        // Arrange: "SAO JOAO" existe em dois estados diferentes, com preços bem distintos
        await using (var seed = CreateContext())
        {
            var saoJoaoSp = CreateStation("01.492.748/0003-83", "Posto A", "SP", "SAO JOAO");
            var saoJoaoMg = CreateStation("11.222.333/0001-81", "Posto B", "MG", "SAO JOAO");
            seed.FuelStations.AddRange(saoJoaoSp, saoJoaoMg);
            await seed.SaveChangesAsync();

            seed.FuelPrices.AddRange(
                FuelPrice.Create(saoJoaoSp, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 5.00m, null, "R$ / litro"),
                FuelPrice.Create(saoJoaoMg, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 9.00m, null, "R$ / litro"));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var readContext = CreateContext();
        var reads = new FuelReads(readContext);
        var result = (await reads.AveragePriceByMunicipality(FuelProduct.Gasoline)).ToList();

        // Assert: duas linhas separadas, mesmo município, preços não se misturam
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Municipality == "SAO JOAO" && r.State == "SP" && r.AveragePrice == 5.00m);
        Assert.Contains(result, r => r.Municipality == "SAO JOAO" && r.State == "MG" && r.AveragePrice == 9.00m);
    }

    [Fact(DisplayName = "AveragePriceByMunicipality filtra por estado quando informado")]
    public async Task AveragePriceByMunicipality_FiltersByState_WhenProvided()
    {
        // Arrange
        await using (var seed = CreateContext())
        {
            var sp = CreateStation("01.492.748/0003-83", "Posto A", "SP", "SAO PAULO");
            var rj = CreateStation("11.222.333/0001-81", "Posto B", "RJ", "NITEROI");
            seed.FuelStations.AddRange(sp, rj);
            await seed.SaveChangesAsync();

            seed.FuelPrices.AddRange(
                FuelPrice.Create(sp, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 5.00m, null, "R$ / litro"),
                FuelPrice.Create(rj, FuelProduct.Gasoline, new DateOnly(2026, 8, 1), 9.00m, null, "R$ / litro"));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var readContext = CreateContext();
        var reads = new FuelReads(readContext);
        var result = (await reads.AveragePriceByMunicipality(FuelProduct.Gasoline, "SP")).ToList();

        // Assert
        var municipality = Assert.Single(result);
        Assert.Equal("SAO PAULO", municipality.Municipality);
        Assert.Equal("SP", municipality.State);
    }
}
