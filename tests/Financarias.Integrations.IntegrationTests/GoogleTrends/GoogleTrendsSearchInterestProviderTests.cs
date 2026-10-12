using Financarias.Application.MarketData.SearchInterest.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Integrations.IntegrationTests.GoogleTrends;

public class GoogleTrendsSearchInterestProviderTests
{
    private readonly ISearchInterestProvider _provider;

    public GoogleTrendsSearchInterestProviderTests()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddIntegrations(configuration);

        _provider = services.BuildServiceProvider().GetRequiredService<ISearchInterestProvider>();
    }

    [Fact(DisplayName = "Lê do Google Trends real o interesse diário de dois termos e vê o pico da Americanas em 12/01/2023")]
    public async Task Get_ReadsTheRealSeries()
    {
        // Act
        var items = await _provider.GetAsync(["americanas", "magalu"], "BR", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        // Assert
        Assert.Equal(31 * 2, items.Count);
        Assert.All(items, item => Assert.InRange(item.Value, 0, 100));

        var peak = items.Where(item => item.Term == "americanas").MaxBy(item => item.Value)!;
        Assert.Equal(new DateOnly(2023, 1, 12), peak.Day);
    }
}
