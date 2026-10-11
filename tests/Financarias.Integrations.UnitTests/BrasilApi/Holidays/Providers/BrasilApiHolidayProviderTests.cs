using System.Text.Json;
using Financarias.Domain.Holidays.Models;
using Financarias.Integrations.BrasilApi.Holidays.Clients;
using Financarias.Integrations.BrasilApi.Holidays.DTOs.Responses;
using Financarias.Integrations.BrasilApi.Holidays.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.BrasilApi.Holidays.Providers;

public class BrasilApiHolidayProviderTests
{
    private readonly IBrasilApiClient _client = Substitute.For<IBrasilApiClient>();
    private readonly BrasilApiHolidayProvider _provider;

    public BrasilApiHolidayProviderTests()
    {
        _provider = new BrasilApiHolidayProvider(_client, NullLogger<BrasilApiHolidayProvider>.Instance);
    }

    [Fact(DisplayName = "Consulta cada ano de 1980 a 2000, uma vez")]
    public async Task Fetch_AsksForEveryYearFrom1980To2000()
    {
        // Arrange
        _client.GetHolidaysAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _provider.FetchHolidaysAsync();

        // Assert
        for (var year = 1980; year <= 2000; year++)
        {
            await _client.Received(1).GetHolidaysAsync(year, Arg.Any<CancellationToken>());
        }

        await _client.DidNotReceive().GetHolidaysAsync(1979, Arg.Any<CancellationToken>());
        await _client.DidNotReceive().GetHolidaysAsync(2001, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Mapeia os feriados nacionais, inclusive os de fim de semana, e deixa a Páscoa de fora")]
    public async Task Fetch_MapsNationalHolidays_KeepingWeekendOnesAndSkippingEaster()
    {
        // Arrange
        _client.GetHolidaysAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _client.GetHolidaysAsync(1990, Arg.Any<CancellationToken>()).Returns(Parse(
            """
            [
              { "date": "1990-01-01", "name": "Confraternização mundial", "type": "national" },
              { "date": "1990-04-15", "name": "Páscoa", "type": "national" },
              { "date": "1990-04-21", "name": "Tiradentes", "type": "national" },
              { "date": "1990-06-14", "name": " Corpus Christi ", "type": "national" }
            ]
            """));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Equal(
            [new DateOnly(1990, 1, 1), new DateOnly(1990, 4, 21), new DateOnly(1990, 6, 14)],
            holidays.Select(h => h.Date));
        Assert.All(holidays, h => Assert.Equal(CountryCode.BR, h.CountryCode));
        Assert.Equal("Corpus Christi", holidays[2].Name);
    }

    [Fact(DisplayName = "Descarta o que não é nacional, não tem nome ou traz data ilegível")]
    public async Task Fetch_DiscardsUnusableRecords()
    {
        // Arrange
        _client.GetHolidaysAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _client.GetHolidaysAsync(1995, Arg.Any<CancellationToken>()).Returns(Parse(
            """
            [
              { "date": "1995-01-25", "name": "Aniversário de São Paulo", "type": "municipal" },
              { "date": "1995-05-01", "name": "", "type": "national" },
              { "date": "05/01/1995", "name": "Dia do trabalho", "type": "national" },
              { "date": "1995-09-07", "name": "Independência do Brasil", "type": "national" }
            ]
            """));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Equal(new DateOnly(1995, 9, 7), Assert.Single(holidays).Date);
    }

    [Fact(DisplayName = "Um ano que falha não derruba os outros: devolve os demais")]
    public async Task Fetch_KeepsTheOtherYears_WhenOneYearFails()
    {
        // Arrange
        _client.GetHolidaysAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _client.GetHolidaysAsync(1985, Arg.Any<CancellationToken>())
            .Returns<List<BrasilApiHoliday>>(_ => throw new HttpRequestException("boom"));
        _client.GetHolidaysAsync(1986, Arg.Any<CancellationToken>()).Returns(Parse(
            """[ { "date": "1986-01-01", "name": "Confraternização mundial", "type": "national" } ]"""));

        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.Equal(new DateOnly(1986, 1, 1), Assert.Single(holidays).Date);
    }

    [Fact(DisplayName = "Cancelamento não é engolido")]
    public async Task Fetch_DoesNotSwallowCancellation()
    {
        // Arrange
        _client.GetHolidaysAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<List<BrasilApiHoliday>>(_ => throw new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _provider.FetchHolidaysAsync());
    }

    private static List<BrasilApiHoliday> Parse(string json) =>
        JsonSerializer.Deserialize<List<BrasilApiHoliday>>(json)!;
}
