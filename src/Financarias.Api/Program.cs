using Financarias.Api.GraphQL;
using Financarias.Application;
using Financarias.Integrations;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddIntegrations(builder.Configuration);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddErrorFilter<DomainErrorFilter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGraphQL();

app.Run();

// Exposed so WebApplicationFactory<Program> can bootstrap the app in functional tests.
public partial class Program { }