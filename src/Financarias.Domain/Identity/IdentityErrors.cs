using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Identity;

public static class IdentityErrors
{
    public const string PasswordHashInvalid = "identity.passwordhash.invalid";
    public const string PasswordMissingDigit = "identity.password.missingdigit";
    public const string PasswordMissingLowercase = "identity.password.missinglowercase";
    public const string PasswordMissingSpecial = "identity.password.missingspecial";
    public const string PasswordMissingUppercase = "identity.password.missinguppercase";
    public const string PasswordTooLong = "identity.password.toolong";
    public const string PasswordTooShort = "identity.password.tooshort";
    public const string UserEmailDuplicate = "identity.user.email.duplicate";
    public const string UserNameRequired = "identity.user.name.required";
    public const string UserNotFound = "identity.user.notfound";

    public static DomainValidationException DuplicateEmail(string email) =>
        new(UserEmailDuplicate, $"Email already in use: '{email}'.");

    public static DomainValidationException InvalidPasswordHash() =>
        new(PasswordHashInvalid, "Password hash is not in a recognizable format.");

    public static DomainValidationException MissingDigit() =>
        new(PasswordMissingDigit, "Password must contain at least one digit.");

    public static DomainValidationException MissingLowercase() =>
        new(PasswordMissingLowercase, "Password must contain at least one lowercase letter.");

    public static DomainValidationException MissingSpecial() =>
        new(PasswordMissingSpecial, $"Password must contain at least one of: {Password.SpecialCharacters}");

    public static DomainValidationException MissingUppercase() =>
        new(PasswordMissingUppercase, "Password must contain at least one uppercase letter.");

    public static DomainValidationException NotFound(Guid id) =>
        new(UserNotFound, $"User not found: '{id}'.");

    public static DomainValidationException PasswordLongerThan(int maximum) =>
        new(PasswordTooLong, $"Password must be at most {maximum} characters.");

    public static DomainValidationException PasswordShorterThan(int minimum) =>
        new(PasswordTooShort, $"Password must be at least {minimum} characters.");

    public static DomainValidationException UserName() =>
        new(UserNameRequired, "User name is required.");
}
