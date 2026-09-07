using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class UserQueriesTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    private WebApplicationFactory<Program> _factory = null!;
    private readonly string _tag = Guid.NewGuid().ToString("N")[..8];
    private Guid _activeId;
    private Guid _inactiveId;
    private string _activeEmail = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Integrations:ViaCep:BaseUrl"] = "https://viacep.com.br/ws",
                    ["Integrations:Anbima:BaseUrl"] = "https://www.anbima.com.br"
                }));

            // A connection string NAO pode vir por ConfigureAppConfiguration: em hosting minimo
            // esse callback roda antes de o CreateBuilder carregar o appsettings.json, entao o
            // Host=localhost;Port=5432 do arquivo vence e a aplicacao fala com o banco local em
            // vez do container. ConfigureTestServices roda depois do registro da aplicacao.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<FinancariasDbContext>>();

                services.AddDbContext<FinancariasDbContext>((provider, options) =>
                    options
                        .UseNpgsql(_postgres.GetConnectionString())
                        .UseSnakeCaseNamingConvention()
                        .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        await db.Database.MigrateAsync();

        _activeEmail = $"ativo-{_tag}@example.com";

        var active = User.Create("Ativo", Email.Create($"Ativo-{_tag}@Example.com"));
        var inactive = User.Create("Inativo", Email.Create($"inativo-{_tag}@example.com"));
        inactive.Deactivate();

        db.Users.AddRange(active, inactive);
        await db.SaveChangesAsync();

        _activeId = active.Id;
        _inactiveId = inactive.Id;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact(DisplayName = "A aplicação sob teste fala com o container, não com o Postgres da máquina")]
    public void Factory_PointsAtTheContainerDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var inUse = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>()
            .Database.GetConnectionString();

        // Assert: sem isto, um appsettings apontando para localhost:5432 faz a suite passar
        // na maquina do dev contra o banco real e quebrar no CI, que nao tem Postgres local.
        Assert.Equal(_postgres.GetConnectionString(), inUse);
    }

    [Fact(DisplayName = "Query user devolve o e-mail achatado em String, com projeção ligada")]
    public async Task User_ReturnsFlattenedEmail_UnderProjection()
    {
        // Act
        var user = await QueryAsync(
            $$"""{ user(id: "{{_activeId}}") { id name email status createdAt updatedAt } }""",
            "user");

        // Assert
        Assert.Equal("Ativo", user.GetProperty("name").GetString());
        Assert.Equal(_activeEmail, user.GetProperty("email").GetString());
        Assert.Equal("ACTIVE", user.GetProperty("status").GetString());
        Assert.NotEqual(default, user.GetProperty("createdAt").GetDateTimeOffset());
    }

    [Fact(DisplayName = "Query users esconde inativo por padrão e mostra com includeInactive")]
    public async Task Users_HidesInactive_ByDefault()
    {
        // Act
        var byDefault = await QueryAsync("{ users { id } }", "users");
        var withInactive = await QueryAsync("{ users(includeInactive: true) { id } }", "users");

        // Assert
        var defaultIds = byDefault.EnumerateArray().Select(u => u.GetProperty("id").GetGuid()).ToList();
        var allIds = withInactive.EnumerateArray().Select(u => u.GetProperty("id").GetGuid()).ToList();

        Assert.Contains(_activeId, defaultIds);
        Assert.DoesNotContain(_inactiveId, defaultIds);
        Assert.Contains(_inactiveId, allIds);
    }

    [Fact(DisplayName = "Query me devolve o usuário do header X-User-Id")]
    public async Task Me_ReturnsUser_FromHeader()
    {
        // Act
        var me = await QueryAsync("{ me { id name email } }", "me", _activeId);

        // Assert
        Assert.Equal(_activeId, me.GetProperty("id").GetGuid());
        Assert.Equal(_activeEmail, me.GetProperty("email").GetString());
    }

    [Fact(DisplayName = "Query me devolve nulo quando não há usuário corrente")]
    public async Task Me_ReturnsNull_WithoutHeader()
    {
        // Act
        var me = await QueryAsync("{ me { id } }", "me");

        // Assert
        Assert.Equal(JsonValueKind.Null, me.ValueKind);
    }

    private async Task<JsonElement> QueryAsync(string query, string field, Guid? currentUser = null)
    {
        var client = _factory.CreateClient();

        if (currentUser is not null)
        {
            client.DefaultRequestHeaders.Add("X-User-Id", currentUser.Value.ToString());
        }

        var response = await client.PostAsJsonAsync("/graphql", new { query });
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        Assert.False(
            document.RootElement.TryGetProperty("errors", out var errors),
            $"GraphQL devolveu erros: {errors}");

        return document.RootElement.GetProperty("data").GetProperty(field).Clone();
    }
}
