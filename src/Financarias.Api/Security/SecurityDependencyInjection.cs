using Financarias.Application.Common.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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

    public static IServiceCollection AddAccessTokenAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, JwtCurrentUser>();

        return services;
    }
}