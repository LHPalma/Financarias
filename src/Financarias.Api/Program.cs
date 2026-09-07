using Financarias.Api.GraphQL;
using Financarias.Api.GraphQL.Types;
using Financarias.Api.Security;
using Financarias.Application;
using Financarias.Application.Common.Security;
using Financarias.Infrastructure;
using Financarias.Infrastructure.Persistence;
using Financarias.Integrations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException(
        "Autenticação real não implementada: a identidade do usuário corrente vem de um header não " +
        "verificado (HeaderCurrentUser), registrado apenas fora de Production. Ver " +
        "docs/srs/2026-07-26-identity-users.md §7.");
}

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()));
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddErrorFilter<DomainErrorFilter>()
    .AddType<FuelPriceType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromMinutes(10);
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });

var app = builder.Build();

// Apply pending EF migrations on startup (dev convenience only).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/graphql"));

app.MapGraphQL();

app.Run();

// Exposed so WebApplicationFactory<Program> can bootstrap the app in functional tests.
public partial class Program
{
}