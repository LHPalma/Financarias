using Financarias.Domain.Analytics.Financing;
using Financarias.Domain.Analytics;
using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.UnitTests.Analytics.Financing;

public class AmortizationScheduleTests
{
    // 30.000 a 1,5% a.m. em 10 parcelas: parcela 3.253,03, juros totais 2.530,25
    private static readonly AmortizationSchedule Schedule = FrenchAmortization.Schedule(
        NominalValue.Create(30000m),
        MonthlyRate.FromFraction(0.015m),
        InstallmentCount.Create(10));

    [Fact(DisplayName = "PayoffAt devolve o saldo devedor da parcela como valor da quitação")]
    public void PayoffAt_WithPeriodInRange_ReturnsOutstandingBalanceOfThatRow()
    {
        // Act
        var payoff = Schedule.PayoffAt(5);

        // Assert
        Assert.Equal(5, payoff.Period);
        Assert.Equal(15558.04m, payoff.OutstandingBalance);
        Assert.Equal(5, payoff.InstallmentsRemaining);
        Assert.Equal(1823.19m, payoff.InterestPaid);
        Assert.Equal(707.06m, payoff.InterestSaved);
    }

    [Theory(DisplayName = "Economia é a soma das parcelas restantes menos o saldo devedor")]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(10)]
    public void PayoffAt_InterestSaved_EqualsRemainingInstallmentsMinusBalance(int period)
    {
        // Arrange — caminho independente do usado na implementação
        var remainingInstallments = Schedule.Rows
            .Skip(period)
            .Sum(row => row.Installment);

        // Act
        var payoff = Schedule.PayoffAt(period);

        // Assert
        Assert.Equal(remainingInstallments - payoff.OutstandingBalance, payoff.InterestSaved);
    }

    [Fact(DisplayName = "Juros pagos mais juros economizados fecham no total de juros")]
    public void PayoffAt_InterestPaidPlusInterestSaved_EqualsTotalInterest()
    {
        // Act
        var payoff = Schedule.PayoffAt(5);

        // Assert
        Assert.Equal(Schedule.TotalInterest, payoff.InterestPaid + payoff.InterestSaved);
    }

    [Fact(DisplayName = "Quitar na última parcela não economiza nada e zera o saldo")]
    public void PayoffAt_LastPeriod_SavesNothing()
    {
        // Act
        var payoff = Schedule.PayoffAt(10);

        // Assert
        Assert.Equal(0m, payoff.OutstandingBalance);
        Assert.Equal(0, payoff.InstallmentsRemaining);
        Assert.Equal(2530.25m, payoff.InterestPaid);
        Assert.Equal(0m, payoff.InterestSaved);
    }

    [Fact(DisplayName = "Quitar mais cedo economiza mais que quitar mais tarde")]
    public void PayoffAt_EarlierPeriod_SavesMoreThanLaterPeriod()
    {
        // Act
        var earlier = Schedule.PayoffAt(3);
        var later = Schedule.PayoffAt(8);

        // Assert
        Assert.True(earlier.InterestSaved > later.InterestSaved);
        Assert.True(earlier.OutstandingBalance > later.OutstandingBalance);
    }

    [Theory(DisplayName = "PayoffAt lança InvalidPayoffPeriodException fora do intervalo 1..n")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void PayoffAt_WithPeriodOutOfRange_ThrowsInvalidPayoffPeriodException(int period)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Schedule.PayoffAt(period));
        Assert.Equal("analytics.financing.payoffperiod.invalid", exception.Code);
    }
}
