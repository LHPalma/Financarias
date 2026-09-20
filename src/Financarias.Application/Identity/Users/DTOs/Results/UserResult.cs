using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.DTOs.Results;

/// <summary>Usuário do sistema, sem nenhum dado de credencial.</summary>
/// <param name="Id">Identificador único do usuário.</param>
/// <param name="Name">Nome de exibição.</param>
/// <param name="Email">E-mail de acesso, em minúsculas.</param>
/// <param name="Status">Situação da conta.</param>
/// <param name="CreatedAt">Instante da criação.</param>
/// <param name="UpdatedAt">Instante da última alteração, inclusive troca de senha e mudança de situação.</param>
public sealed record UserResult(
    Guid Id,
    string Name,
    string Email,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);