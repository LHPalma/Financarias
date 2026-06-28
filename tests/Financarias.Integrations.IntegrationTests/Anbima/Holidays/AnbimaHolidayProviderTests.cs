using Financarias.Application.Holidays.Import;
using Financarias.Domain.Holidays.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Integrations.IntegrationTests.Anbima.Holidays;

public class AnbimaHolidayProviderTests
{
    private readonly IHolidayProvider _provider;

    public AnbimaHolidayProviderTests()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIntegrations(configuration);

        _provider = services.BuildServiceProvider()
            .GetRequiredService<IHolidayProvider>();
    }

    [Fact(DisplayName = "Baixa e parseia a planilha real de feriados da Anbima")]
    public async Task FetchHolidays_DownloadsAndParsesRealAnbimaSpreadsheet()
    {
        // Act
        var holidays = await _provider.FetchHolidaysAsync();

        // Assert
        Assert.True(holidays.Count > 100, $"Esperava centenas de feriados; vieram {holidays.Count}.");
        Assert.All(holidays, holiday =>
        {
            Assert.Equal(CountryCode.BR, holiday.CountryCode);
            Assert.False(string.IsNullOrWhiteSpace(holiday.Name));
        });
    }
}
