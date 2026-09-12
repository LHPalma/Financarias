using Financarias.Application.Common.Persistence;
using Financarias.Application.Common.Security;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Interceptors;
using Financarias.Infrastructure.Persistence.Repositories;
using Financarias.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<FinancariasDbContext>((provider, options) =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<FinancariasDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddOptions<PasswordHashingOptions>()
            .Bind(configuration.GetSection(PasswordHashingOptions.SectionName))
            .Validate(options => options.Peppers.Count > 0,
                "PasswordHashing:Peppers não configurado — nenhum pepper disponível.")
            .Validate(options => options.Peppers.ContainsKey(options.CurrentPepperVersion),
                "PasswordHashing:CurrentPepperVersion não existe em Peppers.")
            .Validate(options => options.Peppers.Values.All(pepper => pepper is { Length: >= 32 }),
                "PasswordHashing: todo pepper precisa de ao menos 32 caracteres.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

        return services;
    }
}