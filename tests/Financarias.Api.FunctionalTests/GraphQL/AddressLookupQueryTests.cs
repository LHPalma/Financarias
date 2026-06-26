using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.Addresses;
using Financarias.Domain.Addresses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class AddressLookupQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IAddressLookupProvider _addressLookupProvider = Substitute.For<IAddressLookupProvider>();
    private readonly WebApplicationFactory<Program> _factory;

    public AddressLookupQueryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Guarantees AddIntegrations does not fail at startup regardless of content root.
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Integrations:ViaCep:BaseUrl"] = "https://viacep.com.br/ws"
                }));

            // Replace the real ViaCEP provider with a stub so the test is deterministic and offline.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAddressLookupProvider>();
                services.AddScoped(_ => _addressLookupProvider);
            });
        });
    }

    [Fact(DisplayName = "Query addressLookup retorna o endereço resolvido pelo provider")]
    public async Task AddressLookup_WithResolvedAddress_ReturnsMappedData()
    {
        // Arrange
        _addressLookupProvider
            .FindAddressAsync(Cep.Create("01001000"), Arg.Any<CancellationToken>())
            .Returns(new AddressLookupResult("Praça da Sé", "Sé", "São Paulo", "SP", "lado ímpar"));

        var client = _factory.CreateClient();
        var request = new { query = "{ addressLookup(cep: \"01001000\") { street neighborhood city state complement } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        var address = document.RootElement.GetProperty("data").GetProperty("addressLookup");
        Assert.Equal("Praça da Sé", address.GetProperty("street").GetString());
        Assert.Equal("Sé", address.GetProperty("neighborhood").GetString());
        Assert.Equal("São Paulo", address.GetProperty("city").GetString());
        Assert.Equal("SP", address.GetProperty("state").GetString());
    }

    [Fact(DisplayName = "Query addressLookup retorna null quando o provider não encontra o endereço")]
    public async Task AddressLookup_WhenProviderReturnsNull_ReturnsNull()
    {
        // Arrange
        _addressLookupProvider
            .FindAddressAsync(Arg.Any<Cep>(), Arg.Any<CancellationToken>())
            .Returns((AddressLookupResult?)null);

        var client = _factory.CreateClient();
        var request = new { query = "{ addressLookup(cep: \"00000000\") { street } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        var addressLookup = document.RootElement.GetProperty("data").GetProperty("addressLookup");
        Assert.Equal(JsonValueKind.Null, addressLookup.ValueKind);
    }

    [Fact(DisplayName = "Query addressLookup retorna erro com code address.cep.invalid para CEP inválido")]
    public async Task AddressLookup_WithInvalidCep_ReturnsDomainErrorCode()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { query = "{ addressLookup(cep: \"123\") { street } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        var raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        var error = document.RootElement.GetProperty("errors")[0];
        Assert.Equal("address.cep.invalid", error.GetProperty("extensions").GetProperty("code").GetString());
    }
}
