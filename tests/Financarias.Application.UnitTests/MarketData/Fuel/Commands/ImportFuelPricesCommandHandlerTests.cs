using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Fuel.Commands;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Fuel.Commands;

public class ImportFuelPricesCommandHandlerTests
{
    private static readonly DateOnly CollectedOn = new(2026, 1, 2);
    private const string Cnpj1 = "01.492.748/0003-83";

    private readonly IFuelPriceProvider _provider = Substitute.For<IFuelPriceProvider>();
    private readonly IRepository<FuelStation> _stations = Substitute.For<IRepository<FuelStation>>();
    private readonly IRepository<FuelPrice> _prices = Substitute.For<IRepository<FuelPrice>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact(DisplayName = "Cria o posto e insere os preços quando nada existe ainda")]
    public async Task Handle_CreatesStationAndPrices_WhenNothingExists()
    {
        // Arrange: dois produtos do mesmo posto (mesmo CNPJ) -> 1 posto, 2 preços
        ProviderReturns(
            Item(product: FuelProduct.Gasoline, salePrice: 7.97m),
            Item(product: FuelProduct.Diesel, salePrice: 8.15m));
        NoExistingStations();
        NoExistingPrices();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new ImportFuelPricesCommand());

        // Assert
        Assert.Equal(2, result.RowsProcessed);
        Assert.Equal(1, result.StationsCreated);
        Assert.Equal(2, result.PricesCreated);
        Assert.Equal(0, result.PricesUpdated);
        await _stations.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<FuelStation>>(added => added.Count() == 1),
            Arg.Any<CancellationToken>());
        await _prices.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<FuelPrice>>(added => added.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Não recria posto que já existe (dedup por CNPJ)")]
    public async Task Handle_DoesNotRecreate_ExistingStation()
    {
        // Arrange
        ProviderReturns(Item(product: FuelProduct.Gasoline));
        ExistingStations(Station());
        NoExistingPrices();
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new ImportFuelPricesCommand());

        // Assert
        Assert.Equal(0, result.StationsCreated);
        Assert.Equal(1, result.PricesCreated);
        await _stations.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<FuelStation>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Atualiza preço existente quando o valor de venda mudou")]
    public async Task Handle_UpdatesPrice_WhenValueChanged()
    {
        // Arrange: mesma coleta (posto+produto+data), valor 7,50 -> 7,97
        var station = Station();
        var existingPrice = FuelPrice.Create(station, FuelProduct.Gasoline, CollectedOn, 7.50m, null, "R$ / litro");
        ExistingStations(station);
        ExistingPrices(existingPrice);
        ProviderReturns(Item(product: FuelProduct.Gasoline, salePrice: 7.97m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new ImportFuelPricesCommand());

        // Assert
        Assert.Equal(0, result.PricesCreated);
        Assert.Equal(1, result.PricesUpdated);
        Assert.Equal(7.97m, existingPrice.SalePrice); // mutado no lugar, via método do agregado
        await _prices.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _prices.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<FuelPrice>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Ignora preço idêntico (nem insere nem atualiza)")]
    public async Task Handle_SkipsPrice_WhenUnchanged()
    {
        // Arrange
        var station = Station();
        var existingPrice = FuelPrice.Create(station, FuelProduct.Gasoline, CollectedOn, 7.97m, null, "R$ / litro");
        ExistingStations(station);
        ExistingPrices(existingPrice);
        ProviderReturns(Item(product: FuelProduct.Gasoline, salePrice: 7.97m));
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new ImportFuelPricesCommand());

        // Assert
        Assert.Equal(0, result.PricesCreated);
        Assert.Equal(0, result.PricesUpdated);
        await _prices.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<FuelPrice>>(),
            Arg.Any<CancellationToken>());
        await _prices.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private ImportFuelPricesCommandHandler CreateHandler() => new(_provider, _stations, _prices, _unitOfWork);

    private void ProviderReturns(params FuelPriceImportItem[] items) =>
        _provider.FetchFuelPricesAsync(Arg.Any<CancellationToken>()).Returns(ToAsync(items));

    private void NoExistingStations() =>
        _stations.ListAsync(Arg.Any<ISpecification<FuelStation>>(), Arg.Any<CancellationToken>())
            .Returns(new List<FuelStation>());

    private void ExistingStations(params FuelStation[] stations) =>
        _stations.ListAsync(Arg.Any<ISpecification<FuelStation>>(), Arg.Any<CancellationToken>())
            .Returns(stations.ToList());

    private void NoExistingPrices() =>
        _prices.ListAsync(Arg.Any<ISpecification<FuelPrice>>(), Arg.Any<CancellationToken>())
            .Returns(new List<FuelPrice>());

    private void ExistingPrices(params FuelPrice[] prices) =>
        _prices.ListAsync(Arg.Any<ISpecification<FuelPrice>>(), Arg.Any<CancellationToken>())
            .Returns(prices.ToList());

    private static async IAsyncEnumerable<FuelPriceImportItem> ToAsync(FuelPriceImportItem[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }

    private static FuelStation Station() =>
        FuelStation.Create(
            Cnpj.Create(Cnpj1),
            "Posto X",
            "IPIRANGA",
            Region.North,
            "AC",
            "CRUZEIRO DO SUL",
            null,
            null,
            null,
            null,
            null);

    private static FuelPriceImportItem Item(
        FuelProduct product = FuelProduct.Gasoline,
        decimal salePrice = 7.97m) =>
        new(
            Cnpj.Create(Cnpj1),
            "Posto X",
            "IPIRANGA",
            Region.North,
            "AC",
            "CRUZEIRO DO SUL",
            null,
            null,
            null,
            null,
            null,
            product,
            CollectedOn,
            salePrice,
            null,
            "R$ / litro");
}