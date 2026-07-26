using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Addresses;

public static class AddressErrors
{
    public const string CepInvalid = "address.cep.invalid";

    public static DomainValidationException Cep(string? cep) =>
        new(CepInvalid, $"CEP not valid: '{cep}'. Expected 8 digits.");
}
