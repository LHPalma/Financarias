namespace Financarias.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    private const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromHours(1);

    public static bool HasStrongSigningKey(JwtOptions options)
    {
        var buffer = new byte[options.SigningKey.Length];

        return Convert.TryFromBase64String(options.SigningKey, buffer, out var written) &&
               written >= MinimumSigningKeyBytes;
    }
}