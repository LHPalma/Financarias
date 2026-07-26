using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Application.Analytics.Financing.Queries;
using Financarias.Domain.Analytics;
using Financarias.Domain.Common.Exceptions;

namespace Financarias.Application.UnitTests.Analytics.Financing.Queries;

public class SimulateEarlyPayoffQueryHandlerTests
{
    private readonly SimulateEarlyPayoffQueryHandler _handler = new(new SimulateEarlyPayoffMapper());

    private static SimulateEarlyPayoffQuery Query(int atInstallment) =>
        new(NominalValue.Create(30000m), MonthlyRate.FromFraction(0.015m), InstallmentCount.Create(10), atInstallment);

    [Fact(DisplayName = "Devolve saldo devedor, parcelas restantes e os dois lados do juro")]
    public async Task HandleAsync_WithPeriodInRange_ReturnsPayoff()
    {
        // Arrange — 30.000 a 1,5% a.m. em 10 parcelas, quitando na 5ª
        var query = Query(5);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(5, result.Period);
        Assert.Equal(15558.04m, result.OutstandingBalance);
        Assert.Equal(5, result.InstallmentsRemaining);
        Assert.Equal(1823.19m, result.InterestPaid);
        Assert.Equal(707.06m, result.InterestSaved);
    }

    [Fact(DisplayName = "Quitar na última parcela não economiza nada")]
    public async Task HandleAsync_WithLastPeriod_SavesNothing()
    {
        // Act
        var result = await _handler.HandleAsync(Query(10));

        // Assert
        Assert.Equal(0m, result.OutstandingBalance);
        Assert.Equal(0, result.InstallmentsRemaining);
        Assert.Equal(0m, result.InterestSaved);
    }

    [Fact(DisplayName = "Parcela fora do prazo do contrato lança InvalidPayoffPeriodException")]
    public async Task HandleAsync_WithPeriodBeyondTerm_ThrowsInvalidPayoffPeriodException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => _handler.HandleAsync(Query(11)));
        Assert.Equal("analytics.financing.payoffperiod.invalid", exception.Code);
    }
}
