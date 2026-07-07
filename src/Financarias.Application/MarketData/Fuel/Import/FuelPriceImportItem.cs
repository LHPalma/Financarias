using Financarias.Domain.Addresses;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Application.MarketData.Fuel.Import;

public sealed record FuelPriceImportItem(
    Cnpj Cnpj,
    string StationName,
    string Brand,
    Region Region,
    string State,
    string Municipality,
    string? Street,
    string? Number,
    string? Complement,
    string? Neighborhood,
    Cep? PostalCode,
    FuelProduct Product,
    DateOnly CollectedOn,
    decimal SalePrice,
    decimal? PurchasePrice,
    string MeasureUnit);
