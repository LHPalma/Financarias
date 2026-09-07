using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Identity;

public static class IdentityErrors
{
    public const string UserEmailDuplicate = "identity.user.email.duplicate";
    public const string UserNameRequired = "identity.user.name.required";

    public static DomainValidationException DuplicateEmail(string email) =>
        new(UserEmailDuplicate, $"Email already in use: '{email}'.");

    public static DomainValidationException UserName() =>
        new(UserNameRequired, "User name is required.");
}