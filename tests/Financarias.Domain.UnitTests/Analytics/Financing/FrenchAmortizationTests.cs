using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Exceptions;
using Financarias.Domain.Analytics.Financing;

namespace Financarias.Domain.UnitTests.Analytics.Financing;

public class FrenchAmortizationTests
{
    // Golden values calculados de forma independente
    public static TheoryData<decimal, decimal, int, decimal> InstallmentGoldenCases => new()
    {
        // principal,  taxa a.m.,  parcelas,  PMT esperada
        { 30000m,      0.015m,     48,        881.25m },
        { 50000m,      0.0199m,    60,        1434.92m },
        { 12000m,      0.0249m,    24,        670.21m },
        { 10000m,      0m,         10,        1000.00m },
    };

    [Theory(DisplayName = "Installment bate com a PMT da anuidade constante (golden values)")]
    [MemberData(nameof(InstallmentGoldenCases))]
    public void Installment_MatchesAnnuityFormula_ForKnownCases(
        decimal principal, decimal monthlyRate, int installments, decimal expected)
    {
        // Act
        var installment = Calculate(principal, monthlyRate, installments).Installment;

        // Assert
        Assert.Equal(expected, installment);
    }

    [Fact(DisplayName = "Taxa zero cai no ramo P/n em vez de dividir por zero")]
    public void Installment_WithZeroRate_DividesPrincipalByInstallments()
    {
        // Act
        var schedule = Calculate(10000m, 0m, 10);

        // Assert
        Assert.Equal(1000m, schedule.Installment);
        Assert.Equal(0m, schedule.TotalInterest);
        Assert.All(schedule.Rows, row => Assert.Equal(0m, row.Interest));
    }

    [Fact(DisplayName = "Primeira parcela separa juros e amortização sobre o principal cheio")]
    public void Schedule_FirstRow_ChargesInterestOnFullPrincipal()
    {
        // Act — 30.000 a 1,5% a.m.: juros = 450,00
        var first = Calculate(30000m, 0.015m, 48).Rows[0];

        // Assert
        Assert.Equal(1, first.Period);
        Assert.Equal(881.25m, first.Installment);
        Assert.Equal(450.00m, first.Interest);
        Assert.Equal(431.25m, first.Amortization);
        Assert.Equal(29568.75m, first.OutstandingBalance);
    }

    [Fact(DisplayName = "Última parcela absorve o resíduo do arredondamento (881,28 em vez de 881,25)")]
    public void Schedule_LastRow_AbsorbsRoundingResidue()
    {
        // Act
        var schedule = Calculate(30000m, 0.015m, 48);
        var last = schedule.Rows[^1];

        // Assert
        Assert.Equal(48, last.Period);
        Assert.Equal(881.28m, last.Installment);
        Assert.Equal(13.02m, last.Interest);
        Assert.Equal(868.26m, last.Amortization);
        Assert.Equal(0m, last.OutstandingBalance);
        Assert.Equal(42300.03m, schedule.TotalPaid);
        Assert.Equal(12300.03m, schedule.TotalInterest);
    }

    [Theory(DisplayName = "Soma das amortizações é exatamente o principal e o saldo final é zero")]
    [MemberData(nameof(InstallmentGoldenCases))]
    public void Schedule_AmortizationsSumToPrincipal_AndBalanceReachesZero(
        decimal principal, decimal monthlyRate, int installments, decimal _)
    {
        // Act
        var schedule = Calculate(principal, monthlyRate, installments);

        // Assert
        Assert.Equal(installments, schedule.Rows.Count);
        Assert.Equal(principal, schedule.Rows.Sum(row => row.Amortization));
        Assert.Equal(0m, schedule.Rows[^1].OutstandingBalance);
    }

    [Theory(DisplayName = "Saldo devedor é estritamente decrescente ao longo da tabela")]
    [MemberData(nameof(InstallmentGoldenCases))]
    public void Schedule_OutstandingBalance_DecreasesEveryPeriod(
        decimal principal, decimal monthlyRate, int installments, decimal _)
    {
        // Act
        var balances = Calculate(principal, monthlyRate, installments).Rows
            .Select(row => row.OutstandingBalance)
            .ToList();

        // Assert
        Assert.Equal(balances.OrderByDescending(balance => balance), balances);
        Assert.Equal(balances.Distinct().Count(), balances.Count);
    }

    [Fact(DisplayName = "Mais parcelas reduzem o valor da parcela e aumentam o total de juros")]
    public void Schedule_LongerTerm_LowersInstallmentAndRaisesTotalInterest()
    {
        // Act
        var shorter = Calculate(30000m, 0.015m, 24);
        var longer = Calculate(30000m, 0.015m, 48);

        // Assert
        Assert.True(longer.Installment < shorter.Installment);
        Assert.True(longer.TotalInterest > shorter.TotalInterest);
    }

    [Fact(DisplayName = "Taxa alta em prazo longo lança UnrepresentableFinancingException em vez de estourar decimal")]
    public void Installment_WhenPowerExceedsDecimalRange_ThrowsUnrepresentableFinancingException()
    {
        // Act & Assert — 1,5^600 passa de decimal.MaxValue
        Assert.Throws<UnrepresentableFinancingException>(() => Calculate(30000m, 0.5m, 600));
    }

    private static AmortizationSchedule Calculate(decimal principal, decimal monthlyRate, int installments) =>
        FrenchAmortization.Schedule(
            NominalValue.Create(principal),
            MonthlyRate.FromFraction(monthlyRate),
            InstallmentCount.Create(installments));
}