using System.IO.Compression;
using System.Security;
using System.Text;
using Financarias.Domain.MarketData.IndexComposition;
using Financarias.Integrations.MarketData.B3.IndexComposition.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financarias.Integrations.UnitTests.MarketData.B3.IndexComposition.Providers;

public class IndexCompositionWorkbookReaderTests
{
    private static readonly string[] Header = ["COD.", "ACAO", "TIPO", "QTDE. TEORICA", "PART. %"];

    private readonly IndexCompositionWorkbookReader _reader = new(NullLogger.Instance);

    [Fact(DisplayName = "Read lê a carteira de uma planilha: ativos, quantidade, peso, redutor e pregão-base")]
    public void Read_ReadsThePortfolio()
    {
        // Arrange
        var file = Workbook(("IBOV", IbovRows()));

        // Act
        var portfolios = _reader.Read(file);

        // Assert
        var portfolio = Assert.Single(portfolios);
        Assert.Equal("IBOV", portfolio.IndexCode);
        Assert.Equal(2018, portfolio.Year);
        Assert.Equal(3, portfolio.Quadrimester);
        Assert.Equal(IndexPortfolioKind.FirstPreview, portfolio.Kind);
        Assert.Equal(new DateOnly(2018, 7, 31), portfolio.BaseDate);
        Assert.Equal(16804141.7293524m, portfolio.Divisor);
        Assert.Equal(2, portfolio.Rows.Count);
        var ambev = portfolio.Rows[0];
        Assert.Equal("ABEV3", ambev.Ticker);
        Assert.Equal("AMBEV S/A", ambev.AssetName);
        Assert.Equal("ON", ambev.ShareType);
        Assert.Equal(4347501920, ambev.TheoreticalQuantity);
        Assert.Equal(6.3388m, ambev.WeightPercent);
    }

    [Fact(DisplayName = "Read lê cada planilha como um índice, pelo nome da aba em maiúsculas")]
    public void Read_ReadsOnePortfolioPerSheet()
    {
        // Arrange
        var file = Workbook(("IBOV", IbovRows()), ("IBxl", IbovRows("1ª Prévia do IBrX 50 para Setembro a Dezembro de 2018")));

        // Act
        var portfolios = _reader.Read(file);

        // Assert
        Assert.Equal(["IBOV", "IBXL"], portfolios.Select(portfolio => portfolio.IndexCode));
    }

    [Fact(DisplayName = "Read aceita cabeçalho acentuado, números em texto e vírgula decimal, e ignora a linha de total")]
    public void Read_AcceptsAccentedHeaderTextNumbersAndSkipsTheTotalRow()
    {
        // Arrange
        string?[][] rows =
        [
            ["VIRADA do IBOVESPA para Janeiro de 2024"],
            ["CÓDIGO", "AÇÃO", "TIPO", "QTDE. TEÓRICA", "PART. %"],
            ["VALE3", "VALE", "ON  NM", "3.995.580.000", "11,5000"],
            ["PETR4", "PETROBRAS", "PN  N2", "4.500.000.000", "8,2"],
            ["QUANTIDADE TEORICA TOTAL", null, null, "8495580000", "19,7"],
        ];

        // Act
        var portfolio = Assert.Single(_reader.Read(Workbook(("IBOV", rows))));

        // Assert
        Assert.Equal(IndexPortfolioKind.Final, portfolio.Kind);
        Assert.Equal(2024, portfolio.Year);
        Assert.Equal(1, portfolio.Quadrimester);
        Assert.Equal(["VALE3", "PETR4"], portfolio.Rows.Select(row => row.Ticker));
        Assert.Equal(3995580000, portfolio.Rows[0].TheoreticalQuantity);
        Assert.Equal(11.5m, portfolio.Rows[0].WeightPercent);
        Assert.Equal("ON NM", ShareTypeOf(portfolio.Rows[0]));
        Assert.Null(portfolio.BaseDate);
        Assert.Null(portfolio.Divisor);
    }

    [Fact(DisplayName = "Read devolve lista vazia para a planilha de entradas e saídas, que não traz carteira")]
    public void Read_ReturnsNothing_ForTheEntriesAndExitsWorkbook()
    {
        // Arrange
        string?[][] rows =
        [
            [" ALTERAÇÕES NA 1ª PRÉVIA NAS CARTEIRAS DOS ÍNDICES DA B3 - SET A DEZ/2018"],
            ["IBOV", "ENTRADAS", "SAÍDAS"],
            ["BTOW3", "ENTRA"],
        ];

        // Act
        var portfolios = _reader.Read(Workbook(("RESUMO", rows)));

        // Assert
        Assert.Empty(portfolios);
    }

    [Fact(DisplayName = "Read abre o ZIP, lê a planilha de carteira e ignora a de entradas e saídas")]
    public void Read_ReadsTheWorkbookInsideAZip()
    {
        // Arrange
        var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "ENTRADAS E SAIDAS - 2ª PREVIA SET2019.xlsx", Workbook(("RESUMO", [["ALTERAÇÕES NA 2ª PREVIA - SET A DEZ/2019"], ["IBOV", "ENTRADAS"]])));
            AddEntry(archive, "PRV_GMCT.XLSX", Workbook(("IBOV", IbovRows())));
            AddEntry(archive, "__MACOSX/._PRV_GMCT.XLSX", new MemoryStream([0, 5, 22, 7]));
        }

        zip.Position = 0;

        // Act
        var portfolios = _reader.Read(zip);

        // Assert
        var portfolio = Assert.Single(portfolios);
        Assert.Equal("IBOV", portfolio.IndexCode);
    }

    [Fact(DisplayName = "Read ignora o pregão-base com data impossível em vez de falhar o arquivo")]
    public void Read_IgnoresAnImpossibleBaseDate()
    {
        // Arrange
        var rows = IbovRows();
        rows[^1] = ["PREGÃO BASE: 31/02/2018"];

        // Act
        var portfolio = Assert.Single(_reader.Read(Workbook(("IBOV", rows))));

        // Assert
        Assert.Null(portfolio.BaseDate);
        Assert.Equal(2, portfolio.Rows.Count);
    }

    private static string ShareTypeOf(Financarias.Application.MarketData.IndexComposition.Import.IndexCompositionRowImportItem row) =>
        string.Join(' ', row.ShareType.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string?[][] IbovRows(string title = "1ª Prévia do IBOVESPA para Setembro a Dezembro de 2018") =>
    [
        [title],
        Header,
        ["ABEV3", "AMBEV S/A", "ON", "4347501920", "6.3388"],
        ["BTOW3", "B2W DIGITAL", "ON      NM", "166541795", "0.35210000000000002"],
        ["VALOR DO REDUTOR", null, null, "16804141.7293524"],
        [],
        ["PREGÃO BASE: 31/07/2018"],
    ];

    private static void AddEntry(ZipArchive archive, string name, Stream content)
    {
        using var entry = archive.CreateEntry(name).Open();
        content.CopyTo(entry);
    }

    private static MemoryStream Workbook(params (string Name, string?[][] Rows)[] sheets)
    {
        var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
                + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
                + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
                + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"
                + string.Concat(sheets.Select((_, index) =>
                    $"<Override PartName=\"/xl/worksheets/sheet{index + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"))
                + "</Types>");

            Write(archive, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");

            Write(archive, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" "
                + "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>"
                + string.Concat(sheets.Select((sheet, index) => $"<sheet name=\"{sheet.Name}\" sheetId=\"{index + 1}\" r:id=\"rId{index + 1}\"/>"))
                + "</sheets></workbook>");

            Write(archive, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + string.Concat(sheets.Select((_, index) =>
                    $"<Relationship Id=\"rId{index + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{index + 1}.xml\"/>"))
                + "</Relationships>");

            for (var index = 0; index < sheets.Length; index++)
            {
                Write(archive, $"xl/worksheets/sheet{index + 1}.xml", SheetXml(sheets[index].Rows));
            }
        }

        stream.Position = 0;

        return stream;
    }

    private static string SheetXml(string?[][] rows)
    {
        var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            xml.Append($"<row r=\"{rowIndex + 1}\">");

            for (var column = 0; column < rows[rowIndex].Length; column++)
            {
                if (rows[rowIndex][column] is { } text)
                {
                    xml.Append($"<c r=\"{(char)('A' + column)}{rowIndex + 1}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(text)}</t></is></c>");
                }
            }

            xml.Append("</row>");
        }

        return xml.Append("</sheetData></worksheet>").ToString();
    }

    private static void Write(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
