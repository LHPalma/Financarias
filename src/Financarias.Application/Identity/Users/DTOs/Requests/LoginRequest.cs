namespace Financarias.Application.Identity.Users.DTOs.Requests;

/// <summary>Credenciais para obter um token de acesso.</summary>
/// <param name="Email">E-mail cadastrado. Espaços nas pontas são ignorados.</param>
/// <param name="Password">Senha em texto puro.</param>
public sealed record LoginRequest(
    string Email,
    string Password);
