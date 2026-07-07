using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Financarias.Domain.LegalEntities;

/// <summary>
///     CNPJ — cadastro nacional de pessoa jurídica, identificador de 14 dígitos de uma empresa.
/// </summary>
public sealed partial record Cnpj
{
    private const int Digits = 14;

    private Cnpj(string value) => Value = value;

    /// <summary>Forma canônica: 14 dígitos, sem máscara (ex.: "01492748000383").</summary>
    public string Value { get; }

    /// <summary>Forma apresentável com máscara (ex.: "01.492.748/0003-83").</summary>
    public string Formatted => $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";

    /// <summary>Cria um <see cref="Cnpj"/> a partir de uma entrada já validada; lança se inválida.</summary>
    public static Cnpj Create(string? input)
    {
        return !TryCreate(input, out var cnpj) ? throw new InvalidCnpjException(input) : cnpj;
    }

    /// <summary>Tenta criar um <see cref="Cnpj"/>; retorna false em vez de lançar quando a entrada é inválida.</summary>
    public static bool TryCreate(string? input, [NotNullWhen(true)] out Cnpj? cnpj)
    {
        cnpj = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var digits = NonDigit().Replace(input, string.Empty);

        if (digits.Length != Digits)
        {
            return false;
        }

        cnpj = new Cnpj(digits);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigit();
}
