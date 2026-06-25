using Financarias.Integrations.Addresses.ViaCep.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Integrations.IntegrationTests.Addresses.ViaCep.Clients;

public class ViaCepClientTests
{
    private readonly IViaCepClient _client;

    public ViaCepClientTests()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIntegrations(configuration);

        _client = services.BuildServiceProvider()
            .GetRequiredService<IViaCepClient>();
    }

    [Fact(DisplayName = "Deve desserializar o endereço para um CEP conhecido")]
    public async Task FindAddressByCep_ReturnsAddress_ForKnownCep()
    {
        // Act
        var response = await _client.FindAddressByCepAsync("01001000");

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Erro is null or false);
        Assert.Equal("São Paulo", response.Localidade);
        Assert.Equal("SP", response.Uf);
        Assert.False(string.IsNullOrWhiteSpace(response.Logradouro));
    }
}