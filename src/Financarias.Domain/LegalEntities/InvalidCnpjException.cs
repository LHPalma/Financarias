using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.LegalEntities;

public sealed class InvalidCnpjException(string? cnpj)
    : BaseDomainException("legalentity.cnpj.invalid", $"CNPJ not valid: '{cnpj}'. Expected 14 digits.");
