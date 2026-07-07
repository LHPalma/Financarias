using Financarias.Domain.Addresses;
using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Domain.MarketData.Fuel.Exceptions;

namespace Financarias.Domain.UnitTests.MarketData.Fuel;

public class FuelStationTests
{
    private static FuelStation CreateStation(string name = "Posto Copacabana") =>
        FuelStation.Create(
            Cnpj.Create("01.492.748/0003-83"),
            name,
            "IPIRANGA",
            Region.North,
            "AC",
            "CRUZEIRO DO SUL",
            "AVENIDA COPACABANA",
            "440",
            null,
            "COPACABANA",
            Cep.Create("69980-000"));

    [Fact(DisplayName = "Create monta o posto com CNPJ, cadastro e endereço")]
    public void Create_WithValidData_SetsProperties()
    {
        // Act
        var station = CreateStation();

        // Assert
        Assert.Equal("01492748000383", station.Cnpj.Value);
        Assert.Equal("Posto Copacabana", station.Name);
        Assert.Equal("IPIRANGA", station.Brand);
        Assert.Equal(Region.North, station.Region);
        Assert.Equal("AC", station.State);
        Assert.Equal("CRUZEIRO DO SUL", station.Municipality);
        Assert.Equal("AVENIDA COPACABANA", station.Street);
        Assert.Equal("440", station.Number);
        Assert.Null(station.Complement);
        Assert.Equal("COPACABANA", station.Neighborhood);
        Assert.Equal("69980000", station.PostalCode!.Value);
    }

    [Theory(DisplayName = "Create lança InvalidFuelStationNameException quando o nome é vazio ou em branco")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsInvalidFuelStationNameException(string? name)
    {
        // Act & Assert
        Assert.Throws<InvalidFuelStationNameException>(() => CreateStation(name!));
    }

    [Fact(DisplayName = "UpdateDetails refresca o cadastro do posto a partir da coleta mais recente")]
    public void UpdateDetails_WithNewData_OverwritesRegistrationFields()
    {
        // Arrange
        var station = CreateStation();

        // Act
        station.UpdateDetails(
            "Posto Novo Nome",
            "VIBRA",
            Region.Southeast,
            "SP",
            "SAO PAULO",
            "AVENIDA PAULISTA",
            "1000",
            "LOJA 2",
            "BELA VISTA",
            Cep.Create("01310-100"));

        // Assert
        Assert.Equal("Posto Novo Nome", station.Name);
        Assert.Equal("VIBRA", station.Brand);
        Assert.Equal(Region.Southeast, station.Region);
        Assert.Equal("SP", station.State);
        Assert.Equal("SAO PAULO", station.Municipality);
        Assert.Equal("AVENIDA PAULISTA", station.Street);
        Assert.Equal("1000", station.Number);
        Assert.Equal("LOJA 2", station.Complement);
        Assert.Equal("BELA VISTA", station.Neighborhood);
        Assert.Equal("01310100", station.PostalCode!.Value);
    }

    [Fact(DisplayName = "UpdateDetails lança InvalidFuelStationNameException quando o nome é em branco")]
    public void UpdateDetails_WithBlankName_ThrowsInvalidFuelStationNameException()
    {
        // Arrange
        var station = CreateStation();

        // Act & Assert
        Assert.Throws<InvalidFuelStationNameException>(() =>
            station.UpdateDetails(
                "   ",
                "VIBRA",
                Region.Southeast,
                "SP",
                "SAO PAULO",
                null,
                null,
                null,
                null,
                null));
    }
}
