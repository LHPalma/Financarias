using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Interceptors;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class CreateUserHandlerTests : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedOn = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _author = Guid.CreateVersion7();
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "O agregado devolvido já vem com a auditoria carimbada, pronto para o mapper")]
    public async Task HandleAsync_ReturnsAggregateAlreadyStamped()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = new CreateUserCommandHandler(new Repository<User>(context));

        // Act
        var user = await handler.HandleAsync(
            new CreateUserCommand("Luiz Palma", Email.Create("carimbado@example.com")));

        // Assert: sem o carimbo chegar na instância, o UserResult sairia com CreatedAt no default
        Assert.Equal(CreatedOn, user.CreatedAt);
        Assert.Equal(CreatedOn, user.UpdatedAt);
        Assert.Equal(_author, user.CreatedBy);
        Assert.Equal(_author, user.UpdatedBy);
    }

    [Fact(DisplayName = "A specification acha o duplicado no banco e o handler lança antes de escrever")]
    public async Task HandleAsync_Throws_WhenEmailAlreadyPersisted()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = new CreateUserCommandHandler(new Repository<User>(context));
        await handler.HandleAsync(new CreateUserCommand("Primeiro", Email.Create("ocupado@example.com")));

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            handler.HandleAsync(new CreateUserCommand("Segundo", Email.Create("  OCUPADO@Example.COM  "))));

        // Assert
        Assert.Equal("identity.user.email.duplicate", exception.Code);
        Assert.False(await context.Users.AnyAsync(u => u.Name == "Segundo"));
    }

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditableEntityInterceptor(
                new StubCurrentUser(_author),
                new FakeTimeProvider(CreatedOn)))
            .Options;

        return new FinancariasDbContext(options);
    }

    private sealed class StubCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;
    }
}
