using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.IndexComposition;

namespace Financarias.Domain.UnitTests.MarketData.IndexComposition;

public class IndexCompositionItemTests
{
    private static IndexCompositionItem Ambev() => IndexCompositionItem.Create(
        "ibov",
        2018,
        3,
        IndexPortfolioKind.FirstPreview,
        "abev3",
        " AMBEV S/A ",
        "ON      NM",
        4347501920,
        6.3388m,
        new DateOnly(2018, 7, 31),
        16804141.7293524m,
        "https://www.b3.com.br/data/files/x.zip");

    [Fact(DisplayName = "Create normaliza o código do índice, o ticker e o tipo da ação")]
    public void Create_NormalizesTheFields()
    {
        // Act
        var item = Ambev();

        // Assert
        Assert.Equal("IBOV", item.IndexCode);
        Assert.Equal("ABEV3", item.Ticker);
        Assert.Equal("AMBEV S/A", item.AssetName);
        Assert.Equal("ON NM", item.ShareType);
        Assert.Equal(new DateOnly(2018, 7, 31), item.BaseDate);
        Assert.Equal(16804141.7293524m, item.Divisor);
    }

    [Fact(DisplayName = "Create aceita carteira sem pregão-base nem redutor")]
    public void Create_AcceptsMissingBaseDateAndDivisor()
    {
        // Act
        var item = IndexCompositionItem.Create("IBOV", 2020, 1, IndexPortfolioKind.Final, "VALE3", "VALE", "ON NM", 1, 10m, null, null, null);

        // Assert
        Assert.Null(item.BaseDate);
        Assert.Null(item.Divisor);
        Assert.Null(item.SourceUrl);
    }

    [Theory(DisplayName = "Create lança erro de domínio quando algum campo é inválido")]
    [InlineData("I", 2018, 3, "ABEV3", "AMBEV", 1, 6.3, null)]
    [InlineData("IBOV", 2009, 3, "ABEV3", "AMBEV", 1, 6.3, null)]
    [InlineData("IBOV", 2018, 0, "ABEV3", "AMBEV", 1, 6.3, null)]
    [InlineData("IBOV", 2018, 4, "ABEV3", "AMBEV", 1, 6.3, null)]
    [InlineData("IBOV", 2018, 3, "AB", "AMBEV", 1, 6.3, null)]
    [InlineData("IBOV", 2018, 3, "ABEV3", " ", 1, 6.3, null)]
    [InlineData("IBOV", 2018, 3, "ABEV3", "AMBEV", 0, 6.3, null)]
    [InlineData("IBOV", 2018, 3, "ABEV3", "AMBEV", 1, -0.1, null)]
    [InlineData("IBOV", 2018, 3, "ABEV3", "AMBEV", 1, 100.1, null)]
    [InlineData("IBOV", 2018, 3, "ABEV3", "AMBEV", 1, 6.3, 0.0)]
    public void Create_WithInvalidField_ThrowsDomainError(
        string indexCode, int year, int quadrimester, string ticker, string assetName, long quantity, double weight, double? divisor)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => IndexCompositionItem.Create(
            indexCode, year, quadrimester, IndexPortfolioKind.Final, ticker, assetName, "ON", quantity, (decimal)weight, null, (decimal?)divisor, null));

        Assert.Equal("indexcomposition.item.invalid", exception.Code);
    }

    [Fact(DisplayName = "Update devolve falso quando nada mudou")]
    public void Update_ReturnsFalse_WhenNothingChanged()
    {
        // Arrange
        var item = Ambev();

        // Act
        var changed = item.Update("AMBEV S/A", "ON NM", 4347501920, 6.3388m, new DateOnly(2018, 7, 31), 16804141.7293524m, "https://www.b3.com.br/data/files/x.zip");

        // Assert
        Assert.False(changed);
    }

    [Fact(DisplayName = "Update grava a quantidade e o peso novos e devolve verdadeiro")]
    public void Update_ReturnsTrue_WhenQuantityChanged()
    {
        // Arrange
        var item = Ambev();

        // Act
        var changed = item.Update("AMBEV S/A", "ON NM", 4348260598, 3.9779m, new DateOnly(2018, 7, 31), 16804141.7293524m, null);

        // Assert
        Assert.True(changed);
        Assert.Equal(4348260598, item.TheoreticalQuantity);
        Assert.Equal(3.9779m, item.WeightPercent);
        Assert.Null(item.SourceUrl);
    }
}
