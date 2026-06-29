using Financarias.Application.Analytics.DTOs.Requests;
using Financarias.Application.Analytics.Mappers;
using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Application.UnitTests.Analytics.Mappers;

public class CalculateNtnbPriceMapperTests
{
    private readonly CalculateNtnbPriceMapper _mapper = new();

    [Fact(DisplayName = "Mapeia o request para a query criando os value objects")]
    public void ToQuery_MapsRequest_CreatingValueObjects()
    {
        // Arrange
        var request = new CalculateNtnbPriceRequest(
            VnaBase: 4300m,
            Yield: 0.07m,
            Inflation: 0.0046m,
            TradeDate: new DateOnly(2024, 5, 21),
            DueDate: new DateOnly(2035, 5, 15));

        // Act
        var query = _mapper.ToQuery(request);

        // Assert
        Assert.Equal(NominalValue.Create(4300m), query.VnaBase);
        Assert.Equal(AnnualYield.FromFraction(0.07m), query.Yield);
        Assert.Equal(0.0046m, query.Inflation);
        Assert.Equal(new DateOnly(2024, 5, 21), query.TradeDate);
        Assert.Equal(new DateOnly(2035, 5, 15), query.DueDate);
    }

    [Fact(DisplayName = "Lança a exceção de domínio quando o VNA é inválido")]
    public void ToQuery_WithInvalidVna_ThrowsDomainException()
    {
        // Arrange — VNA deve ser > 0
        var request = new CalculateNtnbPriceRequest(0m, 0.07m, 0.0046m, new DateOnly(2024, 5, 21), new DateOnly(2035, 5, 15));

        // Act & Assert
        Assert.Throws<InvalidNominalValueException>(() => _mapper.ToQuery(request));
    }
}
