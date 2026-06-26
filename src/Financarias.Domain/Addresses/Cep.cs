using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Financarias.Domain.Addresses;

/// <summary>
///     CEP (Código de Endereçamento Postal) — código postal brasileiro de 8 dígitos.
/// </summary>
public sealed partial record Cep
{
    private const int Digits = 8;

    private Cep(string value) => Value = value;

    /// <summary>Forma canônica: 8 dígitos, sem máscara (ex.: "01001000").</summary>
    public string Value { get; }

    /// <summary>Forma apresentável com máscara (ex.: "01001-000").</summary>
    public string Formatted => $"{Value[..5]}-{Value[5..]}";

    /// <summary>Cria um <see cref="Cep"/> a partir de uma entrada já validada; lança se inválida.</summary>
    public static Cep Create(string? input)
    {
        return !TryCreate(input, out var cep) ? throw new InvalidCepException(input) : cep;
    }

    /// <summary>Tenta criar um <see cref="Cep"/>; retorna false em vez de lançar quando a entrada é inválida.</summary>
    public static bool TryCreate(string? input, [NotNullWhen(true)] out Cep? cep)
    {
        cep = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var digits = NonDigit().Replace(input, string.Empty);

        if (digits.Length != Digits)
        {
            return false;
        }

        cep = new Cep(digits);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigit();
}