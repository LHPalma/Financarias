using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Addresses;

public sealed class InvalidCepException(string? cep)
    : BaseDomainException("address.cep.invalid", $"CEP not valid: '{cep}'. Expected 8 digits.");
