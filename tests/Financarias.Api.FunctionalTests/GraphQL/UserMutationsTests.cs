using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.Common.Security;
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

public class UserMutationsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly string _tag = Guid.NewGuid().ToString("N")[..8];

    private WebApplicationFactory<Program> _factory = null!;
    private string _token = string.Empty;

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

        _token = IssueToken(Guid.CreateVersion7());
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
            $$"""mutation { createUser(input: { name: "Novo Usuário", email: "{{address}}", password: "S3nha-Forte!" }) { id name email status } }""");

        // Assert
        var user = created.GetProperty("data").GetProperty("createUser");
        var id = user.GetProperty("id").GetGuid();

        Assert.Equal("Novo Usuário", user.GetProperty("name").GetString());
        Assert.Equal($"novo-{_tag}@example.com", user.GetProperty("email").GetString());
        Assert.Equal("ACTIVE", user.GetProperty("status").GetString());

        var listed = await ExecuteAsAsync("{ users { id } }");
        var ids = listed.GetProperty("data").GetProperty("users")
            .EnumerateArray().Select(u => u.GetProperty("id").GetGuid());

        Assert.Contains(id, ids);
    }

    [Fact(DisplayName = "createUser com e-mail inválido devolve erro com extensions.code do domínio")]
    public async Task CreateUser_InvalidEmail_ReturnsDomainErrorCode()
    {
        // Act
        var response = await ExecuteAsync(
            """mutation { createUser(input: { name: "Fulano", email: "nao-e-email", password: "S3nha-Forte!" }) { id } }""");

        // Assert
        Assert.Equal("contacts.email.invalid", FirstErrorCode(response));
    }

    [Fact(DisplayName = "createUser com senha fora da política devolve o código da regra")]
    public async Task CreateUser_WeakPassword_ReturnsDomainErrorCode()
    {
        // Act
        var response = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Fraca", email: "fraca-{{_tag}}@example.com", password: "SemDigito!" }) { id } }""");

        // Assert
        Assert.Equal("identity.password.missingdigit", FirstErrorCode(response));
    }

    [Fact(DisplayName = "O hash de senha não existe no schema: pedir o campo é rejeitado")]
    public async Task Users_DoesNotExposeThePasswordHash()
    {
        // Act
        var response = await ExecuteAsAsync("{ users { id passwordHash } }");

        // Assert: o UserType usa BindFieldsExplicitly, então o campo novo do agregado não vaza
        Assert.False(response.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object);
        Assert.Contains("passwordHash", response.GetProperty("errors")[0].GetProperty("message").GetString());
    }

    [Fact(DisplayName = "createUser com e-mail já em uso devolve identity.user.email.duplicate")]
    public async Task CreateUser_DuplicateEmail_ReturnsDomainErrorCode()
    {
        // Arrange
        var address = $"duplicado-{_tag}@example.com";
        await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Primeiro", email: "{{address}}", password: "S3nha-Forte!" }) { id } }""");

        // Act
        var response = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Segundo", email: "{{address}}", password: "S3nha-Forte!" }) { id } }""");

        // Assert
        Assert.Equal("identity.user.email.duplicate", FirstErrorCode(response));
    }


    [Fact(DisplayName = "deactivateUser tira o usuário de users, mas ele volta com includeInactive")]
    public async Task DeactivateUser_RemovesFromDefaultListing()
    {
        // Arrange
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Some", email: "some-{{_tag}}@example.com", password: "S3nha-Forte!" }) { id } }""");
        var id = created.GetProperty("data").GetProperty("createUser").GetProperty("id").GetGuid();

        // Act
        var deactivated = await ExecuteAsAsync(
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
        var response = await ExecuteAsAsync(
            $$"""mutation { deactivateUser(id: "{{Guid.CreateVersion7()}}") { id } }""");

        // Assert
        Assert.Equal("identity.user.notfound", FirstErrorCode(response));
    }

    private async Task<List<Guid>> ListedIdsAsync(string query)
    {
        var response = await ExecuteAsAsync(query);

        return response.GetProperty("data").GetProperty("users")
            .EnumerateArray().Select(u => u.GetProperty("id").GetGuid()).ToList();
    }


    [Fact(DisplayName = "activateUser traz o usuário de volta para a listagem padrão")]
    public async Task ActivateUser_BringsUserBackToDefaultListing()
    {
        // Arrange
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Volta", email: "volta-{{_tag}}@example.com", password: "S3nha-Forte!" }) { id } }""");
        var id = created.GetProperty("data").GetProperty("createUser").GetProperty("id").GetGuid();

        await ExecuteAsAsync($$"""mutation { deactivateUser(id: "{{id}}") { id } }""");
        Assert.DoesNotContain(id, await ListedIdsAsync("{ users { id } }"));

        // Act
        var activated = await ExecuteAsAsync($$"""mutation { activateUser(id: "{{id}}") { id status } }""");

        // Assert
        Assert.Equal(
            "ACTIVE",
            activated.GetProperty("data").GetProperty("activateUser").GetProperty("status").GetString());

        Assert.Contains(id, await ListedIdsAsync("{ users { id } }"));
    }

    [Fact(DisplayName = "deactivateUser sem token é recusado e o usuário continua ativo")]
    public async Task DeactivateUser_IsRejected_WithoutToken()
    {
        // Arrange
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Alvo", email: "alvo-{{_tag}}@example.com", password: "S3nha-Forte!" }) { id } }""");
        var id = created.GetProperty("data").GetProperty("createUser").GetProperty("id").GetGuid();

        // Act
        var response = await ExecuteAsync($$"""mutation { deactivateUser(id: "{{id}}") { id } }""");

        // Assert
        Assert.Equal("AUTH_NOT_AUTHENTICATED", FirstErrorCode(response));
        Assert.Contains(id, await ListedIdsAsync("{ users { id } }"));
    }

    [Fact(DisplayName = "activateUser sem token é recusado e o usuário continua inativo")]
    public async Task ActivateUser_IsRejected_WithoutToken()
    {
        // Arrange
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Parado", email: "parado-{{_tag}}@example.com", password: "S3nha-Forte!" }) { id } }""");
        var id = created.GetProperty("data").GetProperty("createUser").GetProperty("id").GetGuid();
        await ExecuteAsAsync($$"""mutation { deactivateUser(id: "{{id}}") { id } }""");

        // Act
        var response = await ExecuteAsync($$"""mutation { activateUser(id: "{{id}}") { id } }""");

        // Assert
        Assert.Equal("AUTH_NOT_AUTHENTICATED", FirstErrorCode(response));
        Assert.DoesNotContain(id, await ListedIdsAsync("{ users { id } }"));
    }

    [Fact(DisplayName = "createUser, login e me: o token do login abre a área autenticada")]
    public async Task Login_ReturnsTokenThatIdentifiesTheUser_OnMe()
    {
        // Arrange
        var address = $"fluxo-{_tag}@example.com";
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Fluxo", email: "{{address}}", password: "S3nha-Forte!" }) { id } }""");
        var id = created.GetProperty("data").GetProperty("createUser").GetProperty("id").GetGuid();

        // Act
        var login = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{address}}", password: "S3nha-Forte!" }) { accessToken } }""");
        var token = login.GetProperty("data").GetProperty("login").GetProperty("accessToken").GetString();
        var me = await ExecuteAsync("{ me { id email } }", token);

        // Assert
        var user = me.GetProperty("data").GetProperty("me");
        Assert.Equal(id, user.GetProperty("id").GetGuid());
        Assert.Equal(address, user.GetProperty("email").GetString());
    }

    [Fact(DisplayName = "login com senha errada devolve identity.credentials.invalid, sem exigir token")]
    public async Task Login_WrongPassword_ReturnsDomainErrorCode()
    {
        // Arrange
        var address = $"errada-{_tag}@example.com";
        await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Errada", email: "{{address}}", password: "S3nha-Forte!" }) { id } }""");

        // Act
        var response = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{address}}", password: "Outra-Senha1!" }) { accessToken } }""");

        // Assert
        Assert.Equal("identity.credentials.invalid", FirstErrorCode(response));
    }

    [Fact(DisplayName = "login com e-mail inexistente devolve o mesmo código da senha errada")]
    public async Task Login_UnknownEmail_ReturnsTheSameCodeAsWrongPassword()
    {
        // Act
        var response = await ExecuteAsync(
            $$"""mutation { login(input: { email: "ninguem-{{_tag}}@example.com", password: "S3nha-Forte!" }) { accessToken } }""");

        // Assert: distinguir os dois faria do login um oráculo de cadastro (RN-03)
        Assert.Equal("identity.credentials.invalid", FirstErrorCode(response));
    }

    [Fact(DisplayName = "login de usuário desativado, com a senha certa, é recusado como credencial inválida")]
    public async Task Login_InactiveUser_ReturnsDomainErrorCode()
    {
        // Arrange
        var address = $"inativo-{_tag}@example.com";
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "Inativo", email: "{{address}}", password: "S3nha-Forte!" }) { id } }""");
        var id = created.GetProperty("data").GetProperty("createUser").GetProperty("id").GetGuid();
        await ExecuteAsAsync($$"""mutation { deactivateUser(id: "{{id}}") { id } }""");

        // Act
        var response = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{address}}", password: "S3nha-Forte!" }) { accessToken } }""");

        // Assert
        Assert.Equal("identity.credentials.invalid", FirstErrorCode(response));
    }

    [Fact(DisplayName = "login com e-mail malformado devolve identity.credentials.invalid, não o erro de formato")]
    public async Task Login_MalformedEmail_ReturnsCredentialsInvalid()
    {
        // Act
        var response = await ExecuteAsync(
            """mutation { login(input: { email: "nao-e-email", password: "S3nha-Forte!" }) { accessToken } }""");

        // Assert
        Assert.Equal("identity.credentials.invalid", FirstErrorCode(response));
    }

    [Fact(DisplayName = "changePassword troca a senha: a nova entra no login e a antiga deixa de valer")]
    public async Task ChangePassword_SwapsTheCredential()
    {
        // Arrange
        var (address, token) = await CreateUserWithTokenAsync("troca", "Antiga-Senha1!");

        // Act
        var changed = await ExecuteAsync(
            """mutation { changePassword(input: { currentPassword: "Antiga-Senha1!", newPassword: "Nova-Senha2@" }) { id email } }""",
            token);

        // Assert
        Assert.Equal(address, changed.GetProperty("data").GetProperty("changePassword").GetProperty("email").GetString());

        var withNew = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{address}}", password: "Nova-Senha2@" }) { accessToken } }""");
        Assert.NotNull(withNew.GetProperty("data").GetProperty("login").GetProperty("accessToken").GetString());

        var withOld = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{address}}", password: "Antiga-Senha1!" }) { accessToken } }""");
        Assert.Equal("identity.credentials.invalid", FirstErrorCode(withOld));
    }

    [Fact(DisplayName = "changePassword age só sobre o dono do token, sem tocar na senha de outro usuário")]
    public async Task ChangePassword_OnlyAffectsTheTokenOwner()
    {
        // Arrange
        var (ownerAddress, ownerToken) = await CreateUserWithTokenAsync("dono", "Senha-Do-Dono1!");
        var (otherAddress, _) = await CreateUserWithTokenAsync("outro", "Senha-Do-Outro1!");

        // Act
        var changed = await ExecuteAsync(
            """mutation { changePassword(input: { currentPassword: "Senha-Do-Dono1!", newPassword: "Dono-Nova2@" }) { id } }""",
            ownerToken);

        // Assert: sem exigir que a troca do dono aconteceu, o teste passaria mesmo sem a mutation
        Assert.False(changed.TryGetProperty("errors", out var errors), $"GraphQL devolveu erros: {errors}");

        var owner = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{ownerAddress}}", password: "Dono-Nova2@" }) { accessToken } }""");
        Assert.NotNull(owner.GetProperty("data").GetProperty("login").GetProperty("accessToken").GetString());

        var other = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{otherAddress}}", password: "Senha-Do-Outro1!" }) { accessToken } }""");
        Assert.NotNull(other.GetProperty("data").GetProperty("login").GetProperty("accessToken").GetString());
    }

    [Fact(DisplayName = "changePassword com a senha atual errada devolve identity.password.currentincorrect e não troca nada")]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsDomainErrorCode()
    {
        // Arrange
        var (address, token) = await CreateUserWithTokenAsync("errada-atual", "Antiga-Senha1!");

        // Act
        var response = await ExecuteAsync(
            """mutation { changePassword(input: { currentPassword: "Chutada-Senha1!", newPassword: "Nova-Senha2@" }) { id } }""",
            token);

        // Assert
        Assert.Equal("identity.password.currentincorrect", FirstErrorCode(response));

        var stillOld = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{address}}", password: "Antiga-Senha1!" }) { accessToken } }""");
        Assert.NotNull(stillOld.GetProperty("data").GetProperty("login").GetProperty("accessToken").GetString());
    }

    [Fact(DisplayName = "changePassword com a senha nova fora da política devolve o código da regra")]
    public async Task ChangePassword_WeakNewPassword_ReturnsDomainErrorCode()
    {
        // Arrange
        var (_, token) = await CreateUserWithTokenAsync("fraca-nova", "Antiga-Senha1!");

        // Act
        var response = await ExecuteAsync(
            """mutation { changePassword(input: { currentPassword: "Antiga-Senha1!", newPassword: "SemDigito!" }) { id } }""",
            token);

        // Assert
        Assert.Equal("identity.password.missingdigit", FirstErrorCode(response));
    }

    [Fact(DisplayName = "changePassword sem token é recusado e a senha continua a mesma")]
    public async Task ChangePassword_IsRejected_WithoutToken()
    {
        // Arrange
        var (address, _) = await CreateUserWithTokenAsync("sem-token", "Antiga-Senha1!");

        // Act
        var response = await ExecuteAsync(
            """mutation { changePassword(input: { currentPassword: "Antiga-Senha1!", newPassword: "Nova-Senha2@" }) { id } }""");

        // Assert
        Assert.Equal("AUTH_NOT_AUTHENTICATED", FirstErrorCode(response));

        var stillOld = await ExecuteAsync(
            $$"""mutation { login(input: { email: "{{address}}", password: "Antiga-Senha1!" }) { accessToken } }""");
        Assert.NotNull(stillOld.GetProperty("data").GetProperty("login").GetProperty("accessToken").GetString());
    }

    private async Task<(string Address, string Token)> CreateUserWithTokenAsync(string label, string password)
    {
        var address = $"{label}-{_tag}@example.com";
        var created = await ExecuteAsync(
            $$"""mutation { createUser(input: { name: "{{label}}", email: "{{address}}", password: "{{password}}" }) { id } }""");
        var id = created.GetProperty("data").GetProperty("createUser").GetProperty("id").GetGuid();

        return (address, IssueToken(id));
    }

    private static string? FirstErrorCode(JsonElement response) =>
        response.GetProperty("errors")[0].GetProperty("extensions").GetProperty("code").GetString();

    private string IssueToken(Guid userId) =>
        _factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(userId).Token;

    private Task<JsonElement> ExecuteAsAsync(string query) => ExecuteAsync(query, _token);

    private async Task<JsonElement> ExecuteAsync(string query, string? bearerToken = null)
    {
        var client = _factory.CreateClient();

        if (bearerToken is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        var response = await client.PostAsJsonAsync("/graphql", new { query });
        var payload = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(payload).RootElement.Clone();
    }
}
