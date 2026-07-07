using Financarias.Application.MarketData.Fuel.Specifications;
using Financarias.Domain.Addresses;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class FuelPersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly CollectedOn = new(2026, 1, 2);

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

    private static FuelStation SampleStation(string cnpj = "01.492.748/0003-83") =>
        FuelStation.Create(
            Cnpj.Create(cnpj),
            "Posto Copacabana",
            "IPIRANGA",
            Region.North,
            "AC",
            "CRUZEIRO DO SUL",
            "AVENIDA COPACABANA",
            "440",
            null,
            "COPACABANA",
            Cep.Create("69980-000"));

    [Fact(DisplayName = "Persiste e relê posto + preço (round-trip com nav e VOs convertidos)")]
    public async Task SaveAndRead_RoundTripsStationAndPrice()
    {
        // Arrange
        long priceId;
        await using (var write = CreateContext())
        {
            var station = SampleStation();
            write.FuelStations.Add(station);
            await write.SaveChangesAsync();

            var price = FuelPrice.Create(station, FuelProduct.Gasoline, CollectedOn, 7.97m, 6.50m, "R$ / litro");
            write.FuelPrices.Add(price);
            await write.SaveChangesAsync();
            priceId = price.Id;
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.FuelPrices
            .Include(p => p.FuelStation)
            .SingleAsync(p => p.Id == priceId);

        // Assert
        Assert.True(loaded.Id > 0); // gerado pelo banco (identity)
        Assert.True(loaded.StationId > 0);
        Assert.Equal(7.97m, loaded.SalePrice);
        Assert.Equal(6.50m, loaded.PurchasePrice);
        Assert.NotNull(loaded.FuelStation);
        Assert.Equal("01492748000383", loaded.FuelStation.Cnpj.Value);
        Assert.Equal("69980000", loaded.FuelStation.PostalCode!.Value);
        Assert.Equal(Region.North, loaded.FuelStation.Region);
    }

    [Fact(DisplayName = "Índice único barra posto com CNPJ duplicado")]
    public async Task UniqueIndex_RejectsDuplicateStationCnpj()
    {
        // Arrange
        await using var context = CreateContext();
        context.FuelStations.Add(SampleStation());
        await context.SaveChangesAsync();

        // Act
        context.FuelStations.Add(SampleStation());

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(DisplayName = "Índice único barra preço duplicado (posto + produto + data)")]
    public async Task UniqueIndex_RejectsDuplicatePrice()
    {
        // Arrange
        await using var context = CreateContext();
        var station = SampleStation();
        context.FuelStations.Add(station);
        await context.SaveChangesAsync();

        context.FuelPrices.Add(FuelPrice.Create(station, FuelProduct.Gasoline, CollectedOn, 7.97m, null, "R$ / litro"));
        await context.SaveChangesAsync();

        // Act
        context.FuelPrices.Add(FuelPrice.Create(station, FuelProduct.Gasoline, CollectedOn, 8.00m, null, "R$ / litro"));

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(DisplayName = "StationsByCnpjsSpecification traduz Contains sobre o VO Cnpj para SQL")]
    public async Task StationsByCnpjs_TranslatesContainsOverVo()
    {
        // Arrange
        await using (var seed = CreateContext())
        {
            seed.FuelStations.Add(SampleStation("01.492.748/0003-83"));
            seed.FuelStations.Add(SampleStation("11.222.333/0001-81"));
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new Repository<FuelStation>(context);
        var target = Cnpj.Create("01.492.748/0003-83");

        // Act
        var found = await repository.ListAsync(new StationsByCnpjsSpecification([target]));

        // Assert
        Assert.Single(found);
        Assert.Equal(target, found[0].Cnpj);
    }
}
