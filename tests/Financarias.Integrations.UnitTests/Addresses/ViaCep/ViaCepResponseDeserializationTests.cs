using System.Text.Json;
using Financarias.Integrations.Addresses.ViaCep.DTOs.Responses;

namespace Financarias.Integrations.UnitTests.Addresses.ViaCep;

public class ViaCepResponseDeserializationTests
{
    // Mirrors what Refit uses under the hood (case-insensitive web defaults).
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact(DisplayName = "Desserializa endereço válido mapeando os campos da ViaCEP")]
    public void Deserialize_ValidPayload_MapsFields()
    {
        // Arrange
        const string json = """{ "cep": "01001-000", "logradouro": "Praça da Sé", "complemento": "lado ímpar", "bairro": "Sé", "localidade": "São Paulo", "uf": "SP" }""";

        // Act
        var response = JsonSerializer.Deserialize<ViaCepResponse>(json, Options);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Praça da Sé", response!.Logradouro);
        Assert.Equal("Sé", response.Bairro);
        Assert.Equal("São Paulo", response.Localidade);
        Assert.Equal("SP", response.Uf);
        Assert.Null(response.Erro);
    }

    [Fact(DisplayName = "Desserializa o campo 'erro' string da ViaCEP para bool? true (CEP inexistente)")]
    public void Deserialize_NotFoundPayload_MapsErroStringToTrue()
    {
        // Arrange (ViaCEP devolve { "erro": "true" } como STRING para CEP válido inexistente)
        const string json = """{ "erro": "true" }""";

        // Act
        var response = JsonSerializer.Deserialize<ViaCepResponse>(json, Options);

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.Erro);
    }
}
