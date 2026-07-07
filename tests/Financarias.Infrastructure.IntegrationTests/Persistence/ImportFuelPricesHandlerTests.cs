using System.Runtime.CompilerServices;
using Financarias.Application.MarketData.Fuel.Commands;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Domain.Addresses;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class ImportFuelPricesHandlerTests : IAsyncLifetime
{
    private const string Cnpj1 = "01.492.748/0003-83";
    private static readonly DateOnly Week1 = new(2026, 1, 2);
    private static readonly DateOnly Week2 = new(2026, 1, 9);

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

    private async Task<FuelImportResult> RunImportAsync(params FuelPriceImportItem[] items)
    {
        await using var context = CreateContext();
        var handler = new ImportFuelPricesCommandHandler(
            new StubFuelPriceProvider(items),
            new Repository<FuelStation>(context),
            new Repository<FuelPrice>(context));

        return await handler.HandleAsync(new ImportFuelPricesCommand());
    }

    [Fact(DisplayName = "Import cria postos/preços novos e faz upsert real na reimportação (insere, atualiza e pula)")]
    public async Task Import_PersistsThenUpserts_AgainstRealPostgres()
    {
        // Arrange & Act — primeira importação: 1 posto, 2 produtos na semana 1
        var first = await RunImportAsync(
            Item(FuelProduct.Gasoline, Week1, 7.97m),
            Item(FuelProduct.Diesel, Week1, 8.15m));

        // Assert — tudo novo
        Assert.Equal(1, first.StationsCreated);
        Assert.Equal(2, first.PricesCreated);
        Assert.Equal(0, first.PricesUpdated);

        // Act — reimportação: gasolina mudou de preço, diesel igual, e chega a semana 2
        var second = await RunImportAsync(
            Item(FuelProduct.Gasoline, Week1, 8.10m), // mudou 7,97 -> 8,10 (update)
            Item(FuelProduct.Diesel, Week1, 8.15m),   // igual (pula)
            Item(FuelProduct.Gasoline, Week2, 8.20m)); // data nova (insert)

        // Assert — posto não recriado; 1 novo preço, 1 atualizado
        Assert.Equal(0, second.StationsCreated);
        Assert.Equal(1, second.PricesCreated);
        Assert.Equal(1, second.PricesUpdated);

        // Assert — estado final do banco
        await using var verify = CreateContext();
        Assert.Equal(1, await verify.FuelStations.CountAsync());
        Assert.Equal(3, await verify.FuelPrices.CountAsync());

        var updated = await verify.FuelPrices
            .SingleAsync(p => p.Product == FuelProduct.Gasoline && p.CollectedOn == Week1);
        Assert.Equal(8.10m, updated.SalePrice);
    }

    private static FuelPriceImportItem Item(FuelProduct product, DateOnly collectedOn, decimal salePrice) =>
        new(
            Cnpj.Create(Cnpj1),
            "Posto Copacabana",
            "IPIRANGA",
            Region.North,
            "AC",
            "CRUZEIRO DO SUL",
            "AVENIDA COPACABANA",
            "440",
            null,
            "COPACABANA",
            Cep.Create("69980-000"),
            product,
            collectedOn,
            salePrice,
            null,
            "R$ / litro");

    private sealed class StubFuelPriceProvider(IReadOnlyList<FuelPriceImportItem> items) : IFuelPriceProvider
    {
        public async IAsyncEnumerable<FuelPriceImportItem> FetchFuelPricesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }
    }
}
