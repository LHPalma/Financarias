using ClosedXML.Excel;
using Financarias.Domain.Holidays.Models;
using Financarias.Integrations.Anbima.Holidays.Clients;
using Financarias.Integrations.Anbima.Holidays.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;

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
