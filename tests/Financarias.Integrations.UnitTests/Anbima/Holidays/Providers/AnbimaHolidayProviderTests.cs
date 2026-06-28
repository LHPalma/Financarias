using ClosedXML.Excel;
using Financarias.Domain.Holidays.Models;
using Financarias.Integrations.Anbima.Holidays.Clients;
using Financarias.Integrations.Anbima.Holidays.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Financarias.Integrations.UnitTests.Anbima.Holidays.Providers;

public class AnbimaHolidayProviderTests
{
    private readonly IAnbimaHolidayClient _client = Substitute.For<IAnbimaHolidayClient>();
    private readonly AnbimaHolidayProvider _provider;

    public AnbimaHolidayProviderTests()
    {
        _provider = new AnbimaHolidayProvider(_client, Substitute.For<ILogger<AnbimaHolidayProvider>>());
    }

    [Fact(DisplayName = "Parseia as linhas de feriado e ignora o cabeçalho")]
    public async Task Fetch_ParsesHolidayRows_AndSkipsHeader()
    {
        // Arrange
        GivenClientReturns(BuildSpreadsheet(
            ("Data", "Dia da Semana", "Feriado"),
            (new DateTime(2001, 1, 1), "segunda-feira", "Confraternização Universal"),
            (new DateTime(2001, 2, 26), "segunda-feira", "Carnaval")));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Equal(2, holidays.Count);
        Assert.Equal(new DateOnly(2001, 1, 1), holidays[0].Date);
        Assert.Equal("Confraternização Universal", holidays[0].Name);
        Assert.Equal(CountryCode.BR, holidays[0].CountryCode);
        Assert.Equal(new DateOnly(2001, 2, 26), holidays[1].Date);
        Assert.Equal("Carnaval", holidays[1].Name);
    }

    [Fact(DisplayName = "Ignora cabeçalho, linhas em branco e notas de rodapé")]
    public async Task Fetch_SkipsHeaderBlankAndFootnoteRows()
    {
        // Arrange
        GivenClientReturns(BuildSpreadsheet(
            ("Data", "Dia da Semana", "Feriado"),
            (new DateTime(2001, 1, 1), "segunda-feira", "Confraternização Universal"),
            (null, null, null),
            ("4) Esta listagem não inclui os feriados municipais.", null, null),
            (new DateTime(2001, 4, 21), "sábado", "Tiradentes")));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Equal(2, holidays.Count);
        Assert.Equal("Confraternização Universal", holidays[0].Name);
        Assert.Equal("Tiradentes", holidays[1].Name);
    }

    [Fact(DisplayName = "Ignora linha com data mas sem nome de feriado")]
    public async Task Fetch_SkipsRowWithoutHolidayName()
    {
        // Arrange
        GivenClientReturns(BuildSpreadsheet(
            (new DateTime(2001, 1, 1), "segunda-feira", "   "),
            (new DateTime(2001, 4, 21), "sábado", "Tiradentes")));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Single(holidays);
        Assert.Equal("Tiradentes", holidays[0].Name);
    }

    [Fact(DisplayName = "Parseia data vinda como número de série do Excel")]
    public async Task Fetch_ParsesExcelSerialDate()
    {
        // Arrange — célula numérica crua (sem formato de data) cai no caminho do serial/OADate
        var serial = new DateTime(2001, 1, 1).ToOADate();
        GivenClientReturns(BuildSpreadsheet((serial, "segunda-feira", "Confraternização Universal")));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Single(holidays);
        Assert.Equal(new DateOnly(2001, 1, 1), holidays[0].Date);
    }

    [Theory(DisplayName = "Parseia a data em formatos textuais aceitos")]
    [InlineData("1/1/2001", 2001, 1, 1)]
    [InlineData("21/04/2001", 2001, 4, 21)]
    [InlineData("2001-12-25", 2001, 12, 25)]
    public async Task Fetch_ParsesTextualDateFormats(string text, int year, int month, int day)
    {
        // Arrange
        GivenClientReturns(BuildSpreadsheet((text, "dia", "Feriado")));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Single(holidays);
        Assert.Equal(new DateOnly(year, month, day), holidays[0].Date);
    }

    [Fact(DisplayName = "Retorna lista vazia quando o download da planilha falha")]
    public async Task Fetch_WhenClientThrows_ReturnsEmpty()
    {
        // Arrange
        _client.GetHolidaysFileAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Empty(holidays);
    }

    private void GivenClientReturns(Stream spreadsheet) =>
        _client.GetHolidaysFileAsync(Arg.Any<CancellationToken>()).Returns(spreadsheet);

    private static Stream BuildSpreadsheet(params (object? Date, string? DayOfWeek, string? Name)[] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Feriados");

        for (var i = 0; i < rows.Length; i++)
        {
            var (date, dayOfWeek, name) = rows[i];
            var row = i + 1;

            if (date is not null)
            {
                sheet.Cell(row, 1).Value = ToCellValue(date);
            }

            if (dayOfWeek is not null)
            {
                sheet.Cell(row, 2).Value = dayOfWeek;
            }

            if (name is not null)
            {
                sheet.Cell(row, 3).Value = name;
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static XLCellValue ToCellValue(object value) => value switch
    {
        DateTime dateTime => dateTime,
        double number => number,
        string text => text,
        _ => throw new ArgumentException($"Unsupported cell type: {value.GetType()}"),
    };
}
