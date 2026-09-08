using System.Diagnostics.CodeAnalysis;

namespace Financarias.Domain.Identity;

/// <summary>
///     Senha já derivada, em formato PHC. O domínio nunca vê a senha em texto puro —
///     só este envelope, produzido pela porta de hash.
/// </summary>
public sealed record PasswordHash
{
    private const int MinimumDollarSigns = 5;

    private PasswordHash(string value) => Value = value;

    public string Value { get; }

    /// <summary>Cria a partir de uma string PHC; lança se não parecer uma.</summary>
    public static PasswordHash Create(string? input) =>
        !TryCreate(input, out var hash) ? throw IdentityErrors.InvalidPasswordHash() : hash;

    /// <summary>Tenta criar; retorna false em vez de lançar quando a entrada não é PHC.</summary>
    public static bool TryCreate(string? input, [NotNullWhen(true)] out PasswordHash? hash)
    {
        hash = null;

        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith('$') || input.Any(char.IsWhiteSpace))
        {
            return false;
        }

        if (input.Count(c => c == '$') < MinimumDollarSigns)
        {
            return false;
        }

        hash = new PasswordHash(input);
        return true;
    }

    public override string ToString() => "PasswordHash(oculto :3)";
}