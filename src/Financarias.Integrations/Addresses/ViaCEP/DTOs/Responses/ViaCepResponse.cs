using System.Text.Json.Serialization;
using Financarias.Integrations.Common.Json;

namespace Financarias.Integrations.Addresses.ViaCep.DTOs.Responses;

public sealed record ViaCepResponse(
    [property: JsonPropertyName("cep")]
    string? Cep,
    [property: JsonPropertyName("logradouro")]
    string? Logradouro,
    [property: JsonPropertyName("complemento")]
    string? Complemento,
    [property: JsonPropertyName("bairro")]
    string? Bairro,
    [property: JsonPropertyName("localidade")]
    string? Localidade,
    [property: JsonPropertyName("uf")]
    string? Uf,
    [property: JsonPropertyName("erro")]
    [property: JsonConverter(typeof(FlexibleBooleanConverter))]
    bool? Erro);