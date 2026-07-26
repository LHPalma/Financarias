using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.LegalEntities;

public static class LegalEntityErrors
{
    public const string CnpjInvalid = "legalentity.cnpj.invalid";

    public static DomainValidationException Cnpj(string? cnpj) =>
        new(CnpjInvalid, $"CNPJ not valid: '{cnpj}'. Expected 14 digits.");
}
