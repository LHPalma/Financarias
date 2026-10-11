using ClosedXML.Excel;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.WorldBank.Clients;
using Financarias.Integrations.MarketData.WorldBank.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData.WorldBank.Providers;

public class WorldBankIndicatorValueProviderTests
{
    private const string Page =
        "<a href=\"https://thedocs.worldbank.org/en/doc/74e8be41-0050012026/related/CMO-Historical-Data-Monthly.xlsx\">Monthly</a>";

    private readonly IWorldBankPageClient _page = Substitute.For<IWorldBankPageClient>();
    private readonly IWorldBankDocsClient _docs = Substitute.For<IWorldBankDocsClient>();
    private readonly WorldBankIndicatorValueProvider _provider;

    public WorldBankIndicatorValueProviderTests()
    {
        _provider = new WorldBankIndicatorValueProvider(_page, _docs, NullLogger<WorldBankIndicatorValueProvider>.Instance);
        _page.GetCommodityMarketsPageAsync(default).ReturnsForAnyArgs(Page);
    }

    [Fact(DisplayName = "GetYearAsync acha a coluna do minério pelo cabeçalho e lê o mês de cada linha, só do ano pedido")]
    public async Task GetYear_FindsTheIronOreColumn_AndReadsTheMonths()
    {
        // Arrange
        GivenWorkbook(
            ("2025M12", 104.5),
            ("2026M02", 100.965),
            ("2026M01", 107.445454));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.IronOre, 2026);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), items[0].ReferenceDate);
        Assert.Equal(107.445454m, items[0].Value);
        Assert.Equal(new DateOnly(2026, 2, 1), items[1].ReferenceDate);
        Assert.Equal(100.965m, items[1].Value);
        Assert.All(items, item => Assert.Equal(EconomicIndicator.IronOre, item.Indicator));
    }

    [Fact(DisplayName = "GetYearAsync baixa a planilha pelo código da edição que está no link da página")]
    public async Task GetYear_UsesTheReleaseCodeFromThePageLink()
    {
        // Arrange
        GivenWorkbook(("2026M01", 107.0));

        // Act
        await _provider.GetYearAsync(EconomicIndicator.IronOre, 2026);

        // Assert
        await _docs.Received(1).GetMonthlyPricesFileAsync("74e8be41-0050012026", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetYearAsync pula meses sem preço, que a planilha marca com reticências")]
    public async Task GetYear_SkipsMonthsWithoutPrice()
    {
        // Arrange
        GivenWorkbook(("2026M01", "…"), ("2026M02", 100.0));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.IronOre, 2026);

        // Assert
        Assert.Equal(new DateOnly(2026, 2, 1), Assert.Single(items).ReferenceDate);
    }

    [Fact(DisplayName = "GetYearAsync devolve lista vazia quando a página não tem o link da planilha")]
    public async Task GetYear_WithoutLink_ReturnsEmpty()
    {
        // Arrange
        _page.GetCommodityMarketsPageAsync(default).ReturnsForAnyArgs("<html></html>");

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.IronOre, 2026);

        // Assert
        Assert.Empty(items);
        await _docs.DidNotReceiveWithAnyArgs().GetMonthlyPricesFileAsync(default!, default);
    }

    [Fact(DisplayName = "GetYearAsync recusa um indicador que não é do Banco Mundial")]
    public async Task GetYear_RejectsAnIndicatorFromAnotherSource()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _provider.GetYearAsync(EconomicIndicator.Vix, 2026));
    }

    private void GivenWorkbook(params (string Period, object Price)[] rows)
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Mismatch Details").Cell(1, 1).Value = "MISMATCH DETAILS";
        var sheet = workbook.AddWorksheet("Monthly Prices");
        sheet.Cell(1, 1).Value = "World Bank Commodity Price Data (The Pink Sheet)";
        sheet.Cell(5, 2).Value = "Crude oil, Brent";
        sheet.Cell(5, 3).Value = "Iron ore, cfr spot";
        sheet.Cell(6, 3).Value = "($/dmtu)";

        for (var i = 0; i < rows.Length; i++)
        {
            sheet.Cell(7 + i, 1).Value = rows[i].Period;
            sheet.Cell(7 + i, 2).Value = 70.0;

            if (rows[i].Price is double price)
            {
                sheet.Cell(7 + i, 3).Value = price;
            }
            else
            {
                sheet.Cell(7 + i, 3).Value = (string)rows[i].Price;
            }
        }

        var buffer = new MemoryStream();
        workbook.SaveAs(buffer);
        var bytes = buffer.ToArray();

        _docs.GetMonthlyPricesFileAsync(default!, default)
            .ReturnsForAnyArgs(_ => Task.FromResult<Stream>(new MemoryStream(bytes)));
    }
}
