using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class UserPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Persiste e relê um usuário, com o VO Email sobrevivendo ao round-trip")]
    public async Task SaveAndRead_RoundTripsUser()
    {
        // Arrange
        var email = Email.Create("Luiz@Example.com");
        var user = User.Create("Luiz Palma", email);

        await using (var write = CreateContext())
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var found = await read.Users.SingleAsync(u => u.Id == user.Id);

        // Assert
        Assert.Equal(user.Id, found.Id);
        Assert.Equal("Luiz Palma", found.Name);
        Assert.Equal(email, found.Email);
        Assert.Equal("luiz@example.com", found.Email.Value);
        Assert.Equal(UserStatus.Active, found.Status);
    }

    [Fact(DisplayName = "Índice único barra e-mail duplicado depois da normalização")]
    public async Task UniqueIndex_RejectsDuplicate_AfterNormalization()
    {
        // Arrange
        await using var context = CreateContext();
        context.Users.Add(User.Create("Primeiro", Email.Create("duplicado@example.com")));
        await context.SaveChangesAsync();

        // Act: mesma pessoa, outra caixa e com espaços — o VO normaliza antes de chegar no índice
        context.Users.Add(User.Create("Segundo", Email.Create("  DUPLICADO@Example.COM  ")));

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Theory(DisplayName = "O Postgres calcula email_host quebrando no último arroba")]
    [InlineData("luiz@mail.sub.example.com.br", "mail.sub.example.com.br")]
    [InlineData("\"a@b\"@example.com", "example.com")]
    public async Task EmailHost_IsComputedByThePostgres(string address, string expectedHost)
    {
        // Arrange
        var user = User.Create("Luiz", Email.Create(address));

        await using (var write = CreateContext())
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var host = await read.Users
            .Where(u => u.Id == user.Id)
            .Select(u => EF.Property<string>(u, "EmailHost"))
            .SingleAsync();

        // Assert
        Assert.Equal(expectedHost, host);
    }

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }
}
