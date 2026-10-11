using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Integrations.IntegrationTests.ExternalIndicators;

public class ExternalIndicatorValueProviderTests
{
    private readonly IIndicatorValueProvider _provider;

    public ExternalIndicatorValueProviderTests()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddIntegrations(configuration);

        _provider = services.BuildServiceProvider().GetRequiredService<IIndicatorValueProvider>();
    }

    [Theory(DisplayName = "Baixa e parseia a série real de cada indicador do mercado externo")]
    [InlineData(EconomicIndicator.BrentOil, 2020, 200)]
    [InlineData(EconomicIndicator.WtiOil, 2020, 200)]
    [InlineData(EconomicIndicator.Vix, 2020, 200)]
    [InlineData(EconomicIndicator.DollarIndex, 2020, 200)]
    [InlineData(EconomicIndicator.IronOre, 2020, 12)]
    public async Task GetYear_DownloadsAndParsesTheRealSeries(EconomicIndicator indicator, int year, int minimumPoints)
    {
        // Act
        var items = await _provider.GetYearAsync(indicator, year);

        // Assert
        Assert.True(items.Count >= minimumPoints, $"Esperava ao menos {minimumPoints} pontos; vieram {items.Count}.");
        Assert.All(items, item =>
        {
            Assert.Equal(year, item.ReferenceDate.Year);
            Assert.Equal(indicator, item.Indicator);
        });
    }

    [Fact(DisplayName = "O Brent de 2020 tem o piso de 21/04 (US$ 9,12) e fica sempre entre 9 e 75 dólares")]
    public async Task GetYear_Brent2020_HasPlausiblePrices()
    {
        // Act
        var items = await _provider.GetYearAsync(EconomicIndicator.BrentOil, 2020);

        // Assert
        Assert.Equal(9.12m, items.Single(item => item.ReferenceDate == new DateOnly(2020, 4, 21)).Value);
        Assert.All(items, item => Assert.InRange(item.Value, 9m, 75m));
    }
}
