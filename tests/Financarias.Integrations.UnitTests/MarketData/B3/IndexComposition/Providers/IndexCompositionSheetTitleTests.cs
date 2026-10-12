using Financarias.Domain.MarketData.IndexComposition;
using Financarias.Integrations.MarketData.B3.IndexComposition.Providers;

namespace Financarias.Integrations.UnitTests.MarketData.B3.IndexComposition.Providers;

public class IndexCompositionSheetTitleTests
{
    [Theory(DisplayName = "TryParse lê o tipo da carteira, o ano e o quadrimestre do título da planilha")]
    [InlineData("1ª Prévia do IBOVESPA para Setembro a Dezembro de 2018", IndexPortfolioKind.FirstPreview, 2018, 3)]
    [InlineData("2ª Prévia do IBrX Brasil para Setembro a Dezembro de 2019", IndexPortfolioKind.SecondPreview, 2019, 3)]
    [InlineData("3ª Prévia do IBOVESPA para Janeiro a Abril de 2020", IndexPortfolioKind.ThirdPreview, 2020, 1)]
    [InlineData("1ª Prévia do IBOVESPA para Maio a Agosto de 2019", IndexPortfolioKind.FirstPreview, 2019, 2)]
    [InlineData("1 Prévia do IBOVESPA para JANEIRO de 2024", IndexPortfolioKind.FirstPreview, 2024, 1)]
    [InlineData("VIRADA do IBOVESPA para Janeiro de 2022", IndexPortfolioKind.Final, 2022, 1)]
    [InlineData("VIRADA do IBrX 50 para Setembro de 2022", IndexPortfolioKind.Final, 2022, 3)]
    [InlineData("VIRADA do IBOVESPA para Maio de 2021", IndexPortfolioKind.Final, 2021, 2)]
    public void TryParse_ReadsKindYearAndQuadrimester(string title, IndexPortfolioKind kind, int year, int quadrimester)
    {
        // Act
        var parsed = IndexCompositionSheetTitle.TryParse(title);

        // Assert
        Assert.Equal(new IndexCompositionSheetTitle(kind, year, quadrimester), parsed);
    }

    [Theory(DisplayName = "TryParse devolve nulo para título que não é de carteira")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ALTERAÇÕES NA 1ª PRÉVIA NAS CARTEIRAS DOS ÍNDICES DA B3 - SET A DEZ/2018")]
    [InlineData("VIRADA DAS CARTEIRAS DOS ÍNDICES DA B3 - JAN A ABR/2022")]
    [InlineData("Prévia do IBOVESPA para 2018")]
    [InlineData("VIRADA do IBOVESPA para Janeiro")]
    public void TryParse_ReturnsNull_ForNonPortfolioTitle(string? title)
    {
        // Act & Assert
        Assert.Null(IndexCompositionSheetTitle.TryParse(title));
    }
}
