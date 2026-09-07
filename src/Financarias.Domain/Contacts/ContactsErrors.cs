using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Contacts;

public static class ContactsErrors
{
    public const string EmailInvalid = "contacts.email.invalid";

    public static DomainValidationException Email(string? email) =>
        new(EmailInvalid, $"Email not valid: '{email}'.");
}