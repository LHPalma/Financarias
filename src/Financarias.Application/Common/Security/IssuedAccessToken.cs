namespace Financarias.Application.Common.Security;

public sealed record IssuedAccessToken(
    string Token,
    DateTimeOffset ExpiresAt);