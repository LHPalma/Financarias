using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;

namespace Financarias.Domain.Contacts;

/// <summary>
///     Endereço de e-mail em forma canônica — sem espaços nas pontas e em minúsculas.
/// </summary>
public sealed record Email
{
    private const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    /// <summary>Parte após o último "@" — o host do endereço (ex.: "sub.example.com.br").</summary>
    public string Host => Value[(Value.LastIndexOf('@') + 1)..];

    /// <summary>Parte antes do último "@" (ex.: "luiz.palma+tag").</summary>
    public string LocalPart => Value[..Value.LastIndexOf('@')];

    /// <summary>Cria um <see cref="Email" /> a partir de uma entrada crua; lança se inválida.</summary>
    public static Email Create(string? input) =>
        !TryCreate(input, out var email) ? throw ContactsErrors.Email(input) : email;

    /// <summary>Tenta criar um <see cref="Email" />; retorna false em vez de lançar quando a entrada é inválida.</summary>
    public static bool TryCreate(string? input, [NotNullWhen(true)] out Email? email)
    {
        email = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return false;
        }

        if (!MailAddress.TryCreate(normalized, out var address) || address.Address != normalized)
        {
            return false;
        }

        if (!address.Host.Contains('.'))
        {
            return false;
        }

        email = new Email(normalized);
        return true;
    }

    public override string ToString() => Value;
}