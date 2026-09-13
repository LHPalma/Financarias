namespace Financarias.Application.Identity.Users.DTOs.Results;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt);
