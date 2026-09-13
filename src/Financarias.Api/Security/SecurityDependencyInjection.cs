using Financarias.Application.Common.Security;

namespace Financarias.Api.Security;

public static class SecurityDependencyInjection
{
    public static IServiceCollection AddAccessTokens(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer não configurado.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Audience não configurado.")
            .Validate(JwtOptions.HasStrongSigningKey,
                "Jwt:SigningKey precisa ser base64 de ao menos 32 bytes aleatórios (HS256).")
            .Validate(options => options.AccessTokenLifetime > TimeSpan.Zero,
                "Jwt:AccessTokenLifetime precisa ser positivo.")
            .ValidateOnStart();

        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        return services;
    }
}
