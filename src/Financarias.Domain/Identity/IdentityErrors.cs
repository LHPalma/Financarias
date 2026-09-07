using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Identity;

public static class IdentityErrors
{
    public const string UserEmailDuplicate = "identity.user.email.duplicate";
    public const string UserNameRequired = "identity.user.name.required";
    public const string UserNotFound = "identity.user.notfound";

    public static DomainValidationException DuplicateEmail(string email) =>
        new(UserEmailDuplicate, $"Email already in use: '{email}'.");

    public static DomainValidationException NotFound(Guid id) =>
        new(UserNotFound, $"User not found: '{id}'.");

    public static DomainValidationException UserName() =>
        new(UserNameRequired, "User name is required.");
}
