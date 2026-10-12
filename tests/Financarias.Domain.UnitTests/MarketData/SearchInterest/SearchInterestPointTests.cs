using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.SearchInterest;

namespace Financarias.Domain.UnitTests.MarketData.SearchInterest;

public class SearchInterestPointTests
{
    private static readonly DateOnly Start = new(2022, 12, 1);
    private static readonly DateOnly End = new(2023, 3, 1);

    [Fact(DisplayName = "Create monta o ponto com termo normalizado e a chave do grupo comparado em ordem")]
    public void Create_NormalizesTheTerm_AndOrdersTheComparisonKey()
    {
        // Act
        var point = SearchInterestPoint.Create("BR", ["Magalu", " Americanas ", "magalu"], Start, End, "  Americanas ", new DateOnly(2023, 1, 12), 100);

        // Assert
        Assert.Equal("americanas", point.Term);
        Assert.Equal("americanas|magalu", point.ComparisonKey);
        Assert.Equal(100, point.Value);
        Assert.Equal(new DateOnly(2023, 1, 12), point.Day);
    }

    [Fact(DisplayName = "A chave do grupo não depende da ordem nem das maiúsculas dos termos")]
    public void ComparisonKey_IgnoresOrderAndCase()
    {
        // Act & Assert
        Assert.Equal(
            SearchInterestPoint.ComparisonKeyOf(["b", "A"]),
            SearchInterestPoint.ComparisonKeyOf(["a", "B"]));
    }

    [Theory(DisplayName = "Create lança erro de domínio para país, termo, valor ou dia inválidos")]
    [InlineData("br", "americanas", 10, 0)]
    [InlineData("BRA", "americanas", 10, 0)]
    [InlineData("BR", "  ", 10, 0)]
    [InlineData("BR", "americanas", -1, 0)]
    [InlineData("BR", "americanas", 101, 0)]
    [InlineData("BR", "americanas", 10, -1)]
    [InlineData("BR", "americanas", 10, 1000)]
    public void Create_WithInvalidField_ThrowsDomainError(string geo, string term, int value, int dayOffsetFromStart)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            SearchInterestPoint.Create(geo, [term], Start, End, term, Start.AddDays(dayOffsetFromStart), value));

        Assert.Equal("searchinterest.point.invalid", exception.Code);
    }

    [Fact(DisplayName = "Create recusa intervalo invertido e intervalo diário acima de 270 dias")]
    public void Create_WithBadRange_ThrowsDomainError()
    {
        // Act & Assert
        Assert.Throws<DomainValidationException>(() => SearchInterestPoint.Create("BR", ["a"], End, Start, "a", Start, 1));
        Assert.Throws<DomainValidationException>(() => SearchInterestPoint.Create("BR", ["a"], Start, Start.AddDays(270), "a", Start, 1));
        Assert.NotNull(SearchInterestPoint.Create("BR", ["a"], Start, Start.AddDays(269), "a", Start, 1));
    }

    [Fact(DisplayName = "Create aceita região no país (BR-SP)")]
    public void Create_AcceptsARegion()
    {
        // Act
        var point = SearchInterestPoint.Create("BR-SP", ["a"], Start, End, "a", Start, 0);

        // Assert
        Assert.Equal("BR-SP", point.Geo);
    }

    [Fact(DisplayName = "UpdateValue troca o valor e, se inválido, lança erro e preserva o anterior")]
    public void UpdateValue_ReplacesTheValue_AndKeepsItWhenInvalid()
    {
        // Arrange
        var point = SearchInterestPoint.Create("BR", ["a"], Start, End, "a", Start, 10);

        // Act
        point.UpdateValue(20);
        var exception = Assert.Throws<DomainValidationException>(() => point.UpdateValue(101));

        // Assert
        Assert.Equal(20, point.Value);
        Assert.Equal("searchinterest.point.invalid", exception.Code);
    }
}
