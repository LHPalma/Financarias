using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class FindEthanolGasolineParityQueryHandlerTests : IAsyncLifetime
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
        "Compara etanol e gasolina por posto usando a coleta mais recente, e exclui quem não tem os dois produtos")]
    public async Task HandleAsync_ComparesLatestPrices_AgainstRealPostgres()
    {
        // Arrange: posto A tem os dois produtos (uma coleta antiga de etanol pra testar "mais recente");
        // posto B só tem etanol e não deve entrar no relatório
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
                FuelPrice.Create(stationB, FuelProduct.Ethanol, new DateOnly(2026, 8, 1), 4.00m, null, "R$ / litro"));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var readContext = CreateContext();
        var handler = new FindEthanolGasolineParityQueryHandler(readContext);
        var result = await handler.HandleAsync(new FindEthanolGasolineParityQuery());

        // Assert
        var parity = Assert.Single(result);
        Assert.Equal("Posto Copacabana", parity.StationName);
        Assert.Equal(3.50m, parity.EthanolPrice); // usou a coleta de agosto, não a de julho
        Assert.Equal(5.00m, parity.GasolinePrice);
        Assert.Equal(0.70m, parity.Ratio);
        Assert.False(parity.IsEthanolAdvantageous);
    }
}
