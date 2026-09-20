namespace Financarias.Application.Identity.Users.DTOs.Requests;

/// <summary>Senhas para a troca da senha do usuário autenticado.</summary>
/// <param name="CurrentPassword">Senha atual, que precisa conferir com a gravada.</param>
/// <param name="NewPassword">Nova senha. Precisa cumprir a mesma política do cadastro.</param>
public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);