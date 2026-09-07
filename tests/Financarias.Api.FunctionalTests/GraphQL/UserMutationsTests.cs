using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class UserMutationsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly string _tag = Guid.NewGuid().ToString("N")[..8];

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                    ["Integrations:ViaCep:BaseUrl"] = "https://viacep.com.br/ws",
                    ["Integrations:Anbima:BaseUrl"] = "https://www.anbima.com.br"
                }));
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact(DisplayName = "createUser cria o usuário ativo, normaliza o e-mail e ele passa a aparecer em users")]
    public async Task CreateUser_PersistsAndShowsUpInUsers()
    {
        // Arrange
        var address = $"Novo-{_tag}@Example.COM";

        // Act
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Novo Usuário", email: "{{address}}" }) { id name email status } }""");

        // Assert
        var user = created.GetProperty("data").GetProperty("createUser");
        var id = user.GetProperty("id").GetGuid();

        Assert.Equal("Novo Usuário", user.GetProperty("name").GetString());
        Assert.Equal($"novo-{_tag}@example.com", user.GetProperty("email").GetString());
        Assert.Equal("ACTIVE", user.GetProperty("status").GetString());

        var listed = await ExecuteAsync("{ users { id } }");
        var ids = listed.GetProperty("data").GetProperty("users")
            .EnumerateArray().Select(u => u.GetProperty("id").GetGuid());

        Assert.Contains(id, ids);
    }

    [Fact(DisplayName = "createUser com e-mail inválido devolve erro com extensions.code do domínio")]
    public async Task CreateUser_InvalidEmail_ReturnsDomainErrorCode()
    {
        // Act
        var response = await ExecuteAsync(
            """mutation { createUser(input: { name: "Fulano", email: "nao-e-email" }) { id } }""");

        // Assert
        Assert.Equal("contacts.email.invalid", FirstErrorCode(response));
    }

    [Fact(DisplayName = "createUser com e-mail já em uso devolve identity.user.email.duplicate")]
    public async Task CreateUser_DuplicateEmail_ReturnsDomainErrorCode()
    {
        // Arrange
        var address = $"duplicado-{_tag}@example.com";
        await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Primeiro", email: "{{address}}" }) { id } }""");

        // Act
        var response = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Segundo", email: "{{address}}" }) { id } }""");

        // Assert
        Assert.Equal("identity.user.email.duplicate", FirstErrorCode(response));
    }


    [Fact(DisplayName = "deactivateUser tira o usuário de users, mas ele volta com includeInactive")]
    public async Task DeactivateUser_RemovesFromDefaultListing()
    {
        // Arrange
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Some", email: "some-{{_tag}}@example.com" }) { id } }""");
        var id = created.GetProperty("data").GetProperty("createUser").GetProperty("id").GetGuid();

        // Act
        var deactivated = await ExecuteAsync(
            $$"""mutation { deactivateUser(id: "{{id}}") { id status } }""");

        // Assert
        Assert.Equal(
            "INACTIVE",
            deactivated.GetProperty("data").GetProperty("deactivateUser").GetProperty("status").GetString());

        Assert.DoesNotContain(id, await ListedIdsAsync("{ users { id } }"));
        Assert.Contains(id, await ListedIdsAsync("{ users(includeInactive: true) { id } }"));
    }

    [Fact(DisplayName = "deactivateUser em id inexistente devolve identity.user.notfound")]
    public async Task DeactivateUser_UnknownId_ReturnsDomainErrorCode()
    {
        // Act
        var response = await ExecuteAsync(
            $$"""mutation { deactivateUser(id: "{{Guid.CreateVersion7()}}") { id } }""");

        // Assert
        Assert.Equal("identity.user.notfound", FirstErrorCode(response));
    }

    private async Task<List<Guid>> ListedIdsAsync(string query)
    {
        var response = await ExecuteAsync(query);

        return response.GetProperty("data").GetProperty("users")
            .EnumerateArray().Select(u => u.GetProperty("id").GetGuid()).ToList();
    }

    private static string? FirstErrorCode(JsonElement response) =>
        response.GetProperty("errors")[0].GetProperty("extensions").GetProperty("code").GetString();

    private async Task<JsonElement> ExecuteAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        var payload = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(payload).RootElement.Clone();
    }
}
