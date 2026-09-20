namespace Financarias.Domain.Identity;

/// <summary>Situação de uma conta de usuário.</summary>
public enum UserStatus
{
    /// <summary>Conta em uso: pode fazer login e usar a API.</summary>
    Active,

    /// <summary>Conta desativada: não consegue fazer login. Um token já emitido continua valendo até expirar.</summary>
    Inactive,
}