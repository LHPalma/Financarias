using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Domain.Analytics.Financing;
using Financarias.Domain.Analytics;
using Financarias.Domain.Common.Exceptions;

namespace Financarias.Application.UnitTests.Analytics.Financing.Mappers;

public class SimulateEarlyPayoffMapperTests
{
    private readonly SimulateEarlyPayoffMapper _mapper = new();

    [Fact(DisplayName = "ToQuery converte os campos crus em value objects e preserva a parcela alvo")]
    public void ToQuery_WithValidRequest_BuildsValueObjects()
    {
        // Arrange
        var request = new SimulateEarlyPayoffRequest(30000m, 0.015m, 10, 5);

        // Act
        var query = _mapper.ToQuery(request);

        // Assert
        Assert.Equal(NominalValue.Create(30000m), query.Principal);
        Assert.Equal(MonthlyRate.FromFraction(0.015m), query.MonthlyRate);
        Assert.Equal(InstallmentCount.Create(10), query.Installments);
        Assert.Equal(5, query.AtInstallment);
    }

    [Fact(DisplayName = "ToQuery propaga InvalidMonthlyRateException para taxa negativa")]
    public void ToQuery_WithNegativeRate_ThrowsInvalidMonthlyRateException()
    {
        // Arrange
        var request = new SimulateEarlyPayoffRequest(30000m, -0.01m, 10, 5);

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => _mapper.ToQuery(request));
        Assert.Equal("analytics.monthlyrate.invalid", exception.Code);
    }

    [Fact(DisplayName = "ToResult copia todos os campos da quitação para o DTO")]
    public void ToResult_WithPayoff_CopiesEveryField()
    {
        // Arrange
        var payoff = new EarlyPayoff(5, 15558.04m, 5, 1823.19m, 707.06m);

        // Act
        var result = _mapper.ToResult(payoff);

        // Assert
        Assert.Equal(5, result.Period);
        Assert.Equal(15558.04m, result.OutstandingBalance);
        Assert.Equal(5, result.InstallmentsRemaining);
        Assert.Equal(1823.19m, result.InterestPaid);
        Assert.Equal(707.06m, result.InterestSaved);
    }
}
