using Financarias.Integrations.Addresses.ViaCep.Clients;
using Financarias.Integrations.Addresses.ViaCep.DTOs.Responses;
using Financarias.Integrations.Addresses.ViaCep.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Financarias.Integrations.UnitTests.Addresses.ViaCep.Providers;

public class ViaCepProviderTests
{
    private readonly IViaCepClient _client;
    private readonly ViaCepProvider _provider;

    public ViaCepProviderTests()
    {
        _client = Substitute.For<IViaCepClient>();
        _provider = new ViaCepProvider(_client, Substitute.For<ILogger<ViaCepProvider>>());
    }

    [Fact(DisplayName = "Mapeia a resposta da ViaCEP para o endereço interno")]
    public async Task FindAddress_WithValidCep_ReturnsMappedAddress()
    {
        // Arrange
        _client.FindAddressByCepAsync("01001000", Arg.Any<CancellationToken>())
            .Returns(new ViaCepResponse(
                "01001-000",
                "Praça da Sé",
                "lado ímpar",
                "Sé",
                "São Paulo",
                "SP",
                null));

        // Act
        var result = await _provider.FindAddressAsync("01001000");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Praça da Sé", result!.Street);
        Assert.Equal("Sé", result.Neighborhood);
        Assert.Equal("São Paulo", result.City);
        Assert.Equal("SP", result.State);
        Assert.Equal("lado ímpar", result.Complement);
    }

    [Fact(DisplayName = "Retorna nulo quando a ViaCEP sinaliza CEP inexistente")]
    public async Task FindAddress_WhenCepNotFound_ReturnsNull()
    {
        // Arrange
        _client.FindAddressByCepAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ViaCepResponse(null, null, null, null, null, null, true));

        // Act
        var result = await _provider.FindAddressAsync("00000000");

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "Retorna nulo quando a chamada à ViaCEP lança exceção")]
    public async Task FindAddress_WhenClientThrows_ReturnsNull()
    {
        // Arrange
        _client.FindAddressByCepAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));

        // Act
        var result = await _provider.FindAddressAsync("01001000");

        // Assert
        Assert.Null(result);
    }
}