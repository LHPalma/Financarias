using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.Inflation;

namespace Financarias.Domain.UnitTests.MarketData.Inflation;

public class InflationIndexTests
{
    private static readonly IReadOnlySet<DateOnly> NoHolidays = new HashSet<DateOnly>();

    private static InflationIndex Build(IReadOnlySet<DateOnly>? holidays = null) =>
        InflationIndex.FromMonthlyRates(
            [
                (new DateOnly(2024, 1, 1), 10m),
                (new DateOnly(2024, 2, 1), 20m),
                (new DateOnly(2024, 3, 1), 5m),
            ],
            holidays ?? NoHolidays);

    [Fact(DisplayName = "O índice vai do dia 15 do primeiro mês ao dia 15 seguinte ao último")]
    public void Range_SpansFromTheFirstFifteenthToTheOneAfterTheLastMonth()
    {
        // Act
        var index = Build();

        // Assert
        Assert.Equal(new DateOnly(2024, 1, 15), index.FirstDate);
        Assert.Equal(new DateOnly(2024, 4, 15), index.LastDate);
    }

    [Fact(DisplayName = "Cada dia 15 carrega o número-índice do mês anterior: o IPCA de janeiro só é atingido em 15/02")]
    public void Level_AtEachFifteenth_IsTheCumulativeIndexOfThePreviousMonth()
    {
        // Act
        var index = Build();

        // Assert
        Assert.Equal(1m, index.Level(new DateOnly(2024, 1, 15)));
        Assert.Equal(1.10m, index.Level(new DateOnly(2024, 2, 15)));
        Assert.Equal(1.32m, index.Level(new DateOnly(2024, 3, 15)));
        Assert.Equal(1.386m, index.Level(new DateOnly(2024, 4, 15)));
    }

    [Fact(DisplayName = "No meio da janela o nível é pró-rata geométrico em dias úteis: 12 de 23 dias úteis de janeiro")]
    public void Level_InsideTheWindow_IsGeometricProRataByBusinessDays()
    {
        // Act
        var level = Build().Level(new DateOnly(2024, 1, 31));

        // Assert: de 16/01 a 31/01 há 12 dias úteis, e de 16/01 a 15/02 há 23
        var expected = Math.Pow(1.10, 12.0 / 23.0);
        Assert.Equal(expected, (double)level, 9);
    }

    [Fact(DisplayName = "Fim de semana não acumula: sábado e domingo têm o nível da sexta")]
    public void Level_OnTheWeekend_EqualsTheFridayLevel()
    {
        // Arrange
        var index = Build();

        // Act & Assert: 19/01/2024 é sexta
        var friday = index.Level(new DateOnly(2024, 1, 19));
        Assert.Equal(friday, index.Level(new DateOnly(2024, 1, 20)));
        Assert.Equal(friday, index.Level(new DateOnly(2024, 1, 21)));
        Assert.True(index.Level(new DateOnly(2024, 1, 22)) > friday);
    }

    [Fact(DisplayName = "Feriado não acumula e reduz o total de dias úteis da janela")]
    public void Level_WithAHoliday_ChangesTheBusinessDayCount()
    {
        // Arrange: 25/01/2024 como feriado deixa a janela de janeiro com 22 dias úteis
        var index = Build(new HashSet<DateOnly> { new(2024, 1, 25) });

        // Act
        var level = index.Level(new DateOnly(2024, 1, 31));

        // Assert: antes eram 12 de 23; agora são 11 de 22
        Assert.Equal(Math.Pow(1.10, 11.0 / 22.0), (double)level, 9);
        Assert.Equal(index.Level(new DateOnly(2024, 1, 24)), index.Level(new DateOnly(2024, 1, 25)));
    }

    [Fact(DisplayName = "Dia 15 em feriado passa para o próximo dia útil, e a janela começa nele")]
    public void AdjustedFifteenth_MovesToTheNextBusinessDay()
    {
        // Arrange: 15/02/2024 é quinta e vira feriado, então a janela de fevereiro começa em 16/02
        var index = Build(new HashSet<DateOnly> { new(2024, 2, 15) });

        // Act & Assert
        Assert.Equal(1.10m, index.Level(new DateOnly(2024, 2, 16)));
        Assert.True(index.Level(new DateOnly(2024, 2, 14)) < 1.10m);
        Assert.Equal(index.Level(new DateOnly(2024, 2, 14)), index.Level(new DateOnly(2024, 2, 15)));
    }

    [Fact(DisplayName = "Fator entre duas datas é a razão dos níveis: entre 15/01 e 15/03 acumula janeiro e fevereiro")]
    public void Factor_IsTheRatioOfTheLevels()
    {
        // Act
        var factor = Build().Factor(new DateOnly(2024, 1, 15), new DateOnly(2024, 3, 15));

        // Assert
        Assert.Equal(1.32m, factor);
    }

    [Fact(DisplayName = "Fator para uma data-base anterior deflaciona: é o inverso do fator para frente")]
    public void Factor_Backwards_IsTheInverse()
    {
        // Arrange
        var index = Build();
        var from = new DateOnly(2024, 1, 15);
        var to = new DateOnly(2024, 3, 15);

        // Act & Assert
        Assert.Equal(1m, index.Factor(from, to) * index.Factor(to, from), 20);
    }

    [Theory(DisplayName = "Data fora do intervalo do índice lança erro de domínio")]
    [InlineData("2024-01-14")]
    [InlineData("2024-04-16")]
    [InlineData("2023-12-01")]
    public void Level_OutsideTheRange_ThrowsDomainError(string date)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Build().Level(DateOnly.Parse(date)));
        Assert.Equal("inflation.date.unavailable", exception.Code);
    }

    [Fact(DisplayName = "ToPresentValue converte a moeda da época e corrige: 2.750 CR$ de 30/06/1994 valem R$ 1,00")]
    public void ToPresentValue_ConvertsTheCurrencyThenAppliesTheFactor()
    {
        // Arrange: taxas zero em 1994, então o fator é 1 e só a conversão atua
        var index = InflationIndex.FromMonthlyRates(
            [(new DateOnly(1994, 5, 1), 0m), (new DateOnly(1994, 6, 1), 0m), (new DateOnly(1994, 7, 1), 0m)],
            NoHolidays);

        // Act
        var value = index.ToPresentValue(2_750m, new DateOnly(1994, 6, 30), new DateOnly(1994, 7, 15));

        // Assert
        Assert.Equal(1m, value);
    }

    [Fact(DisplayName = "ToPresentValue corrige pela inflação: R$ 100 de 15/01 valem R$ 132 em 15/03")]
    public void ToPresentValue_AppliesTheInflation()
    {
        // Act
        var value = Build().ToPresentValue(100m, new DateOnly(2024, 1, 15), new DateOnly(2024, 3, 15));

        // Assert
        Assert.Equal(132m, value);
    }

    [Fact(DisplayName = "A série não pode ter buraco entre os meses")]
    public void FromMonthlyRates_WithAGap_ThrowsDomainError()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            InflationIndex.FromMonthlyRates(
                [(new DateOnly(2024, 1, 1), 1m), (new DateOnly(2024, 3, 1), 1m)],
                NoHolidays));

        Assert.Equal("inflation.series.invalid", exception.Code);
    }

    [Fact(DisplayName = "A série não pode estar vazia")]
    public void FromMonthlyRates_WithNoMonth_ThrowsDomainError()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => InflationIndex.FromMonthlyRates([], NoHolidays));

        Assert.Equal("inflation.series.invalid", exception.Code);
    }

    [Theory(DisplayName = "Taxa mensal de -100% ou menos lança erro de domínio")]
    [InlineData(-100)]
    [InlineData(-150)]
    public void FromMonthlyRates_WithRateAtOrBelowMinusHundred_ThrowsDomainError(double rate)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            InflationIndex.FromMonthlyRates([(new DateOnly(2024, 1, 1), (decimal)rate)], NoHolidays));

        Assert.Equal("inflation.series.invalid", exception.Code);
    }

    [Fact(DisplayName = "Taxa negativa (deflação) é aceita")]
    public void FromMonthlyRates_AcceptsDeflation()
    {
        // Act
        var index = InflationIndex.FromMonthlyRates(
            [(new DateOnly(2026, 8, 1), -0.32m), (new DateOnly(2026, 9, 1), 0.82m)],
            NoHolidays);

        // Assert
        Assert.True(index.Level(index.LastDate) > 1m);
        Assert.Equal(0.9968m * 1.0082m, index.Level(index.LastDate));
    }
}
