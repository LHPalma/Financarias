using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData;

namespace Financarias.Domain.UnitTests.MarketData;

public class QuotePeriodTests
{
    [Fact(DisplayName = "ForDay cria o período de um pregão com a data informada")]
    public void ForDay_WithValidDate_ReturnsDaily()
    {
        // Act
        var period = QuotePeriod.ForDay(new DateOnly(2026, 10, 9));

        // Assert
        var daily = Assert.IsType<QuotePeriod.Daily>(period);
        Assert.Equal(new DateOnly(2026, 10, 9), daily.Date);
    }

    [Fact(DisplayName = "ForDay lança erro de domínio para data anterior ao primeiro arquivo (1986)")]
    public void ForDay_WithDateBeforeFirstYear_ThrowsDomainValidationException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => QuotePeriod.ForDay(new DateOnly(1985, 12, 31)));
        Assert.Equal("stock.quoteperiod.invalid", exception.Code);
    }

    [Theory(DisplayName = "ForMonth cria o período de um mês com ano e mês informados")]
    [InlineData(1986, 1)]
    [InlineData(2026, 9)]
    [InlineData(2026, 12)]
    public void ForMonth_WithValidYearAndMonth_ReturnsMonthly(int year, int month)
    {
        // Act
        var period = QuotePeriod.ForMonth(year, month);

        // Assert
        var monthly = Assert.IsType<QuotePeriod.Monthly>(period);
        Assert.Equal(year, monthly.Year);
        Assert.Equal(month, monthly.Month);
    }

    [Theory(DisplayName = "ForMonth lança erro de domínio para mês fora de 1–12 ou ano anterior a 1986")]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(2026, -1)]
    [InlineData(1985, 12)]
    public void ForMonth_WithInvalidYearOrMonth_ThrowsDomainValidationException(int year, int month)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => QuotePeriod.ForMonth(year, month));
        Assert.Equal("stock.quoteperiod.invalid", exception.Code);
    }

    [Theory(DisplayName = "ForYear cria o período de um ano inteiro")]
    [InlineData(1986)]
    [InlineData(2026)]
    public void ForYear_WithValidYear_ReturnsYearly(int year)
    {
        // Act
        var period = QuotePeriod.ForYear(year);

        // Assert
        var yearly = Assert.IsType<QuotePeriod.Yearly>(period);
        Assert.Equal(year, yearly.Year);
    }

    [Theory(DisplayName = "ForYear lança erro de domínio para ano anterior a 1986")]
    [InlineData(1985)]
    [InlineData(0)]
    [InlineData(-2026)]
    public void ForYear_WithYearBeforeFirstYear_ThrowsDomainValidationException(int year)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => QuotePeriod.ForYear(year));
        Assert.Equal("stock.quoteperiod.invalid", exception.Code);
    }

    [Fact(DisplayName = "Dois períodos da mesma forma e do mesmo valor são iguais (igualdade por valor)")]
    public void Equality_WithSameShapeAndValue_AreEqual()
    {
        // Act & Assert
        Assert.Equal(QuotePeriod.ForMonth(2026, 9), QuotePeriod.ForMonth(2026, 9));
        Assert.Equal(QuotePeriod.ForMonth(2026, 9).GetHashCode(), QuotePeriod.ForMonth(2026, 9).GetHashCode());
    }

    [Fact(DisplayName = "Períodos de formas diferentes não são iguais, mesmo com o mesmo ano")]
    public void Equality_WithDifferentShapes_AreNotEqual()
    {
        // Act & Assert
        Assert.NotEqual(QuotePeriod.ForYear(2026), QuotePeriod.ForMonth(2026, 1));
    }
}
