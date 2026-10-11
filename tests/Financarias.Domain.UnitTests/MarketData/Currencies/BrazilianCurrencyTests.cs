using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.Currencies;

namespace Financarias.Domain.UnitTests.MarketData.Currencies;

public class BrazilianCurrencyTests
{
    [Theory(DisplayName = "EraOf acha a moeda de cada data, inclusive nos dois lados de cada troca")]
    [InlineData("2026-10-09", "Real", "R$", 1)]
    [InlineData("1994-07-01", "Real", "R$", 1)]
    [InlineData("1994-06-30", "Cruzeiro Real", "CR$", 2_750)]
    [InlineData("1993-08-01", "Cruzeiro Real", "CR$", 2_750)]
    [InlineData("1993-07-31", "Cruzeiro", "CR$", 2_750_000)]
    [InlineData("1990-03-16", "Cruzeiro", "CR$", 2_750_000)]
    [InlineData("1990-03-15", "Cruzado Novo", "NCZ$", 2_750_000)]
    [InlineData("1989-01-16", "Cruzado Novo", "NCZ$", 2_750_000)]
    [InlineData("1989-01-15", "Cruzado", "CZ$", 2_750_000_000)]
    [InlineData("1986-02-28", "Cruzado", "CZ$", 2_750_000_000)]
    [InlineData("1986-02-27", "Cruzeiro", "CR$", 2_750_000_000_000)]
    [InlineData("1970-05-15", "Cruzeiro", "CR$", 2_750_000_000_000)]
    public void EraOf_FindsTheCurrencyOfEachDate(string date, string name, string modRef, long divisor)
    {
        // Act
        var era = BrazilianCurrency.EraOf(DateOnly.Parse(date));

        // Assert
        Assert.Equal(name, era.Name);
        Assert.Equal(modRef, era.ModRef);
        Assert.Equal(divisor, era.DivisorToReais);
    }

    [Fact(DisplayName = "As eras cobrem o período sem buracos nem sobreposição")]
    public void Eras_AreContiguous()
    {
        // Arrange
        var ordered = BrazilianCurrency.Eras.OrderBy(era => era.From).ToList();

        // Assert
        Assert.Equal(BrazilianCurrency.FirstSupportedDate, ordered[0].From);
        Assert.Equal(DateOnly.MaxValue, ordered[^1].To);
        for (var i = 1; i < ordered.Count; i++)
        {
            Assert.Equal(ordered[i - 1].To.AddDays(1), ordered[i].From);
        }
    }

    [Fact(DisplayName = "EraOf lança erro de domínio para data anterior à primeira suportada")]
    public void EraOf_BeforeTheFirstSupportedDate_ThrowsDomainError()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => BrazilianCurrency.EraOf(new DateOnly(1970, 5, 14)));
        Assert.Equal("currency.date.unsupported", exception.Code);
    }

    [Fact(DisplayName = "ToReais converte a última cotação do Cruzeiro Real no mesmo R$ 1,00 do primeiro dia do Real")]
    public void ToReais_MakesTheRealConversionExact()
    {
        // Act
        var lastCruzeiroReal = BrazilianCurrency.ToReais(2_750m, new DateOnly(1994, 6, 30));
        var firstReal = BrazilianCurrency.ToReais(1m, new DateOnly(1994, 7, 1));

        // Assert
        Assert.Equal(1m, lastCruzeiroReal);
        Assert.Equal(1m, firstReal);
    }

    [Theory(DisplayName = "Dólar real nos dois lados de cada troca de moeda fica contínuo depois de convertido")]
    [InlineData("1986-02-27", 13_750.0, "1986-03-03", 13.84)]
    [InlineData("1993-07-30", 71_153.0, "1993-08-02", 72.069)]
    [InlineData("1994-06-30", 2_750.0, "1994-07-01", 1.0)]
    public void ToReais_KeepsTheRealDollarSeriesContinuousAcrossRedenominations(
        string before, double beforeValue, string after, double afterValue)
    {
        // Act
        var converted = BrazilianCurrency.ToReais((decimal)beforeValue, DateOnly.Parse(before));
        var convertedAfter = BrazilianCurrency.ToReais((decimal)afterValue, DateOnly.Parse(after));

        // Assert: sem a conversão a razão seria de 1000 ou mais; com ela, é uma variação diária de câmbio
        var ratio = (double)(convertedAfter / converted);
        Assert.InRange(ratio, 0.98, 1.02);
    }

    [Fact(DisplayName = "ToReais devolve o valor exato, sem arredondar valores minúsculos da era do Cruzeiro")]
    public void ToReais_KeepsTinyValuesExact()
    {
        // Act
        var dollarOn1984 = BrazilianCurrency.ToReais(2_828m, new DateOnly(1984, 11, 28));

        // Assert
        Assert.Equal(2_828m / 2_750_000_000_000m, dollarOn1984);
        Assert.True(dollarOn1984 > 0m);
    }

    [Theory(DisplayName = "IsLegalTenderOn confere o código da moeda do arquivo com a data")]
    [InlineData("R$", "2026-10-09", true)]
    [InlineData("CR$", "1993-01-04", true)]
    [InlineData("CR$", "1994-03-01", true)]
    [InlineData("CR$", "1986-01-02", true)]
    [InlineData("CZ$", "1986-03-04", true)]
    [InlineData("NCZ$", "1990-01-02", true)]
    [InlineData("R$", "1993-01-04", false)]
    [InlineData("CR$", "2026-10-09", false)]
    [InlineData("CZ$", "1990-01-02", false)]
    [InlineData("US$", "2026-10-09", false)]
    [InlineData("R$", "1960-01-04", false)]
    public void IsLegalTenderOn_MatchesTheFileCodeWithTheDate(string modRef, string date, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, BrazilianCurrency.IsLegalTenderOn(modRef, DateOnly.Parse(date)));
    }
}
