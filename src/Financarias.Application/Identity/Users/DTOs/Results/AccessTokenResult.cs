namespace Financarias.Application.Identity.Users.DTOs.Results;

/// <summary>Token de acesso emitido no login.</summary>
/// <param name="AccessToken">JWT assinado. Enviar no header Authorization, como Bearer.</param>
/// <param name="ExpiresAt">Instante em que o token deixa de valer.</param>
public sealed record AccessTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt);
