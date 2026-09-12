using System.Diagnostics.CodeAnalysis;

namespace Financarias.Domain.Identity;

/// <summary>
///     Senha já derivada, em formato PHC. O domínio nunca vê a senha em texto puro —
///     só este envelope, produzido pela porta de hash.
/// </summary>
public sealed record PasswordHash
{
    private const int MinimumDollarSigns = 5;

    private PasswordHash(string value, int pepperVersion)
    {
        Value = value;
        PepperVersion = pepperVersion;
    }

    public string Value { get; }

    public int PepperVersion { get; }

    /// <summary>Cria a partir de uma string PHC e da versão do pepper que a derivou; lança se inválido.</summary>
    public static PasswordHash Create(string? input, int pepperVersion) =>
        !TryCreate(input, pepperVersion, out var hash) ? throw IdentityErrors.InvalidPasswordHash() : hash;

    /// <summary>Tenta criar; retorna false em vez de lançar quando a entrada não é PHC ou a versão é inválida.</summary>
    public static bool TryCreate(string? input, int pepperVersion, [NotNullWhen(true)] out PasswordHash? hash)
    {
        hash = null;

        if (pepperVersion < 1)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith('$') || input.Any(char.IsWhiteSpace))
        {
            return false;
        }

        if (input.Count(c => c == '$') < MinimumDollarSigns)
        {
            return false;
        }

        hash = new PasswordHash(input, pepperVersion);
        return true;
    }

    public override string ToString() => "PasswordHash(oculto :3)";
}