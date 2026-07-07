using Financarias.Domain.Addresses;
using Financarias.Domain.Common;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel.Exceptions;

namespace Financarias.Domain.MarketData.Fuel;

public sealed class FuelStation : BaseEntity<long>, IAggregateRoot
{
    private FuelStation()
    {
    }

    private FuelStation(
        Cnpj cnpj,
        string name,
        string brand,
        Region region,
        string state,
        string municipality,
        string? street,
        string? number,
        string? complement,
        string? neighborhood,
        Cep? postalCode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidFuelStationNameException();
        }

        Cnpj = cnpj;
        Name = name;
        Brand = brand;
        Region = region;
        State = state;
        Municipality = municipality;
        Street = street;
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
        PostalCode = postalCode;
    }

    public Cnpj Cnpj { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Brand { get; private set; } = null!;

    public Region Region { get; private set; }

    public string State { get; private set; } = null!;

    public string Municipality { get; private set; } = null!;

    public string? Street { get; private set; }

    public string? Number { get; private set; }

    public string? Complement { get; private set; }

    public string? Neighborhood { get; private set; }

    public Cep? PostalCode { get; private set; }

    public static FuelStation Create(
        Cnpj cnpj,
        string name,
        string brand,
        Region region,
        string state,
        string municipality,
        string? street,
        string? number,
        string? complement,
        string? neighborhood,
        Cep? postalCode) =>
        new(cnpj, name, brand, region, state, municipality, street, number, complement, neighborhood, postalCode);

    public void UpdateDetails(
        string name,
        string brand,
        Region region,
        string state,
        string municipality,
        string? street,
        string? number,
        string? complement,
        string? neighborhood,
        Cep? postalCode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidFuelStationNameException();
        }

        Name = name;
        Brand = brand;
        Region = region;
        State = state;
        Municipality = municipality;
        Street = street;
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
        PostalCode = postalCode;
    }
}
