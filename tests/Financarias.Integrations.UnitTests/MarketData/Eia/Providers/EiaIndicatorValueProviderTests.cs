using ClosedXML.Excel;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Eia.Clients;
using Financarias.Integrations.MarketData.Eia.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData.Eia.Providers;

public class EiaIndicatorValueProviderTests
{
    private readonly IEiaClient _client = Substitute.For<IEiaClient>();
    private readonly EiaIndicatorValueProvider _provider;

    public EiaIndicatorValueProviderTests()
    {
        _provider = new EiaIndicatorValueProvider(_client, NullLogger<EiaIndicatorValueProvider>.Instance);
    }

    [Fact(DisplayName = "GetYearAsync lê a aba Data 1, pula o cabeçalho e devolve só o ano pedido em ordem de data")]
    public async Task GetYear_ReadsTheDataSheet_ForTheRequestedYear()
    {
        // Arrange
        GivenWorkbook(
            (new DateTime(2025, 12, 31), 61.5),
            (new DateTime(2026, 1, 6), 62.10),
            (new DateTime(2026, 1, 5), 63.00));

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.BrentOil, 2026);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(new DateOnly(2026, 1, 5), items[0].ReferenceDate);
        Assert.Equal(63.00m, items[0].Value);
        Assert.Equal(62.10m, items[1].Value);
        Assert.All(items, item => Assert.Equal(EconomicIndicator.BrentOil, item.Indicator));
    }

    [Theory(DisplayName = "GetYearAsync pede à EIA o arquivo do indicador")]
    [InlineData(EconomicIndicator.BrentOil, "RBRTEd.xls")]
    [InlineData(EconomicIndicator.WtiOil, "RWTCd.xls")]
    public async Task GetYear_RequestsTheIndicatorFile(EconomicIndicator indicator, string file)
    {
        // Arrange
        GivenWorkbook((new DateTime(2026, 1, 5), 63.00));

        // Act
        await _provider.GetYearAsync(indicator, 2026);

        // Assert
        await _client.Received(1).GetSpotPricesFileAsync(file, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetYearAsync devolve lista vazia quando a planilha não tem a aba Data 1")]
    public async Task GetYear_WithoutDataSheet_ReturnsEmpty()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Contents").Cell(1, 1).Value = "x";
        GivenStream(workbook);

        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.BrentOil, 2026);

        // Assert
        Assert.Empty(items);
    }

    [Fact(DisplayName = "GetYearAsync recusa um indicador que não é da EIA")]
    public async Task GetYear_RejectsAnIndicatorFromAnotherSource()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _provider.GetYearAsync(EconomicIndicator.Vix, 2026));
    }

    private void GivenWorkbook(params (DateTime Date, double Price)[] rows)
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Contents").Cell(1, 1).Value = "Workbook Contents";
        var sheet = workbook.AddWorksheet("Data 1");
        sheet.Cell(1, 2).Value = "Data 1: Europe Brent Spot Price FOB (Dollars per Barrel)";
        sheet.Cell(2, 1).Value = "Sourcekey";
        sheet.Cell(3, 1).Value = "Date";
        sheet.Cell(3, 2).Value = "Europe Brent Spot Price FOB (Dollars per Barrel)";

        for (var i = 0; i < rows.Length; i++)
        {
            sheet.Cell(4 + i, 1).Value = rows[i].Date;
            sheet.Cell(4 + i, 2).Value = rows[i].Price;
        }

        GivenStream(workbook);
    }

    private void GivenStream(XLWorkbook workbook)
    {
        var buffer = new MemoryStream();
        workbook.SaveAs(buffer);
        var bytes = buffer.ToArray();

        _client.GetSpotPricesFileAsync(default!, default)
            .ReturnsForAnyArgs(_ => Task.FromResult<Stream>(new MemoryStream(bytes)));
    }
}
