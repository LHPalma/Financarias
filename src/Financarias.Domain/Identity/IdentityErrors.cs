using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Identity;

public static class IdentityErrors
{
    public const string UserNameRequired = "identity.user.name.required";

    public static DomainValidationException UserName() =>
        new(UserNameRequired, "User name is required.");
}