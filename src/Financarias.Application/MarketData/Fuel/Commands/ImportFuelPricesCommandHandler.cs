using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Fuel.Specifications;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Application.MarketData.Fuel.Commands;

public class ImportFuelPricesCommandHandler(
    IFuelPriceProvider provider,
    IRepository<FuelStation> stationRepository,
    IRepository<FuelPrice> priceRepository
) : ICommandHandler<ImportFuelPricesCommand, FuelImportResult>
{
    private const int BatchSize = 5000;

    public async Task<FuelImportResult> HandleAsync(
        ImportFuelPricesCommand command,
        CancellationToken cancellationToken = default)
    {
        var totals = default(Counters);
        var batch = new List<FuelPriceImportItem>(BatchSize);

        await foreach (var item in provider.FetchFuelPricesAsync(cancellationToken))
        {
            batch.Add(item);

            if (batch.Count < BatchSize)
            {
                continue;
            }

            totals += await FlushAsync(batch, cancellationToken);
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            totals += await FlushAsync(batch, cancellationToken);
        }

        return new FuelImportResult(
            totals.Rows,
            totals.StationsCreated,
            totals.PricesCreated,
            totals.PricesUpdated);
    }

    private static bool TryApplyChanges(FuelPrice price, FuelPriceImportItem item)
    {
        if (price.SalePrice == item.SalePrice &&
            price.PurchasePrice == item.PurchasePrice &&
            price.MeasureUnit == item.MeasureUnit)
        {
            return false;
        }

        price.UpdateValues(item.SalePrice, item.PurchasePrice, item.MeasureUnit);
        return true;
    }

    private static FuelPrice CreatePrice(FuelStation station, FuelPriceImportItem item) =>
        FuelPrice.Create(
            station,
            item.Product,
            item.CollectedOn,
            item.SalePrice,
            item.PurchasePrice,
            item.MeasureUnit);

    private async Task<Counters> FlushAsync(
        IReadOnlyList<FuelPriceImportItem> batch,
        CancellationToken cancellationToken)
    {
        var stationsByCnpj = await EnsureStationsAsync(batch, cancellationToken);
        var (created, updated) = await UpsertPricesAsync(batch, stationsByCnpj, cancellationToken);

        return new Counters(batch.Count, stationsByCnpj.NewlyCreated, created, updated);
    }

    private async Task<StationMap> EnsureStationsAsync(
        IReadOnlyList<FuelPriceImportItem> batch,
        CancellationToken cancellationToken)
    {
        var cnpjs = batch.Select(item => item.Cnpj).Distinct().ToList();

        var stations = (await stationRepository.ListAsync(new StationsByCnpjsSpecification(cnpjs), cancellationToken))
            .ToDictionary(station => station.Cnpj);

        var newStations = new List<FuelStation>();
        foreach (var item in batch)
        {
            if (stations.ContainsKey(item.Cnpj))
            {
                continue;
            }

            var station = FuelStation.Create(
                item.Cnpj,
                item.StationName,
                item.Brand,
                item.Region,
                item.State,
                item.Municipality,
                item.Street,
                item.Number,
                item.Complement,
                item.Neighborhood,
                item.PostalCode);

            stations[item.Cnpj] = station;
            newStations.Add(station);
        }

        if (newStations.Count > 0)
        {
            await stationRepository.AddRangeAsync(newStations, cancellationToken);
        }

        return new StationMap(stations, newStations.Count);
    }

    private async Task<(int Created, int Updated)> UpsertPricesAsync(
        IReadOnlyList<FuelPriceImportItem> batch,
        StationMap stations,
        CancellationToken cancellationToken)
    {
        var priceByKey = await LoadExistingPricesAsync(batch, stations, cancellationToken);

        var newPrices = new List<FuelPrice>();
        var updated = 0;

        foreach (var item in batch)
        {
            var station = stations[item.Cnpj];
            var key = (station.Id, item.Product, item.CollectedOn);

            if (priceByKey.TryGetValue(key, out var existing))
            {
                if (TryApplyChanges(existing, item))
                {
                    updated++;
                }
            }
            else
            {
                var created = CreatePrice(station, item);
                priceByKey[key] = created;
                newPrices.Add(created);
            }
        }

        await PersistPricesAsync(newPrices, updated, cancellationToken);

        return (newPrices.Count, updated);
    }

    private async Task<Dictionary<(long StationId, FuelProduct Product, DateOnly CollectedOn), FuelPrice>>
        LoadExistingPricesAsync(
            IReadOnlyList<FuelPriceImportItem> batch,
            StationMap stations,
            CancellationToken cancellationToken)
    {
        var stationIds = stations.Values.Select(station => station.Id).Distinct().ToList();
        var collectedDates = batch.Select(item => item.CollectedOn).Distinct().ToList();

        var existingPrices = await priceRepository.ListAsync(
            new PricesByStationsAndDatesSpecification(stationIds, collectedDates),
            cancellationToken);

        return existingPrices.ToDictionary(price => (price.StationId, price.Product, price.CollectedOn));
    }

    private async Task PersistPricesAsync(
        IReadOnlyList<FuelPrice> newPrices,
        int updated,
        CancellationToken cancellationToken)
    {
        if (newPrices.Count > 0)
        {
            await priceRepository.AddRangeAsync(newPrices, cancellationToken);
        }
        else if (updated > 0)
        {
            await priceRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record StationMap(Dictionary<Cnpj, FuelStation> Stations, int NewlyCreated)
    {
        public IEnumerable<FuelStation> Values => Stations.Values;

        public FuelStation this[Cnpj cnpj] => Stations[cnpj];
    }

    private readonly record struct Counters(int Rows, int StationsCreated, int PricesCreated, int PricesUpdated)
    {
        public static Counters operator +(Counters a, Counters b) =>
            new(
                a.Rows + b.Rows,
                a.StationsCreated + b.StationsCreated,
                a.PricesCreated + b.PricesCreated,
                a.PricesUpdated + b.PricesUpdated);
    }
}