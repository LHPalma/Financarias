using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Application.UnitTests.Analytics.Financing.Mappers;

public class SimulateFinancingMapperTests
{
    private readonly SimulateFinancingMapper _mapper = new();

    [Fact(DisplayName = "ToQuery converte os campos crus em value objects do domínio")]
    public void ToQuery_WithValidRequest_BuildsValueObjects()
    {
        // Arrange
        var request = new SimulateFinancingRequest(30000m, 0.015m, 48);

        // Act
        var query = _mapper.ToQuery(request);

        // Assert
        Assert.Equal(NominalValue.Create(30000m), query.Principal);
        Assert.Equal(MonthlyRate.FromFraction(0.015m), query.MonthlyRate);
        Assert.Equal(InstallmentCount.Create(48), query.Installments);
    }

    [Fact(DisplayName = "ToQuery propaga InvalidNominalValueException para principal não positivo")]
    public void ToQuery_WithNonPositivePrincipal_ThrowsInvalidNominalValueException()
    {
        // Arrange
        var request = new SimulateFinancingRequest(0m, 0.015m, 48);

        // Act & Assert
        Assert.Throws<InvalidNominalValueException>(() => _mapper.ToQuery(request));
    }

    [Fact(DisplayName = "ToQuery propaga InvalidMonthlyRateException para taxa negativa")]
    public void ToQuery_WithNegativeRate_ThrowsInvalidMonthlyRateException()
    {
        // Arrange
        var request = new SimulateFinancingRequest(30000m, -0.01m, 48);

        // Act & Assert
        Assert.Throws<InvalidMonthlyRateException>(() => _mapper.ToQuery(request));
    }

    [Fact(DisplayName = "ToQuery propaga InvalidInstallmentCountException para prazo fora do intervalo")]
    public void ToQuery_WithOutOfRangeInstallments_ThrowsInvalidInstallmentCountException()
    {
        // Arrange
        var request = new SimulateFinancingRequest(30000m, 0.015m, 0);

        // Act & Assert
        Assert.Throws<InvalidInstallmentCountException>(() => _mapper.ToQuery(request));
    }
}
