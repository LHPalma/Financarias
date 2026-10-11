using Financarias.Application.MarketData.Issuers.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Integrations.IntegrationTests.B3.Issuers;

public class B3IssuerProviderTests
{
    private readonly IIssuerProvider _provider;

    public B3IssuerProviderTests()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddIntegrations(configuration);

        _provider = services.BuildServiceProvider().GetRequiredService<IIssuerProvider>();
    }

    [Fact(DisplayName = "Lê da B3 o cadastro real e a classificação setorial da Petrobras e da Vale, e conta uma empresa extinta como não listada")]
    public async Task Get_ReadsTheRealRegistry()
    {
        // Act
        var fetch = await _provider.GetAsync(["PETR", "VALE", "ARCZ"]);

        // Assert
        var petrobras = fetch.Items.Single(item => item.IssuerCode == "PETR");
        Assert.Equal("9512", petrobras.CvmCode);
        Assert.Equal("33000167000101", petrobras.Cnpj);
        Assert.Contains("Petróleo", petrobras.Sector, StringComparison.Ordinal);
        Assert.NotNull(petrobras.Segment);

        var vale = fetch.Items.Single(item => item.IssuerCode == "VALE");
        Assert.Equal("Materiais Básicos", vale.Sector);
        Assert.Equal("Mineração", vale.Subsector);

        Assert.Equal(1, fetch.NotListed);
    }
}
