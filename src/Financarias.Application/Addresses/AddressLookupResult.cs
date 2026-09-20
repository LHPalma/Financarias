namespace Financarias.Application.Addresses;

/// <summary>Endereço de um CEP. Os campos são nulos quando a fonte não os informa.</summary>
/// <param name="Street">Logradouro (rua, avenida...).</param>
/// <param name="Neighborhood">Bairro.</param>
/// <param name="City">Município.</param>
/// <param name="State">Sigla da UF, com duas letras.</param>
/// <param name="Complement">Complemento do logradouro.</param>
public sealed record AddressLookupResult(
    string? Street,
    string? Neighborhood,
    string? City,
    string? State,
    string? Complement
);