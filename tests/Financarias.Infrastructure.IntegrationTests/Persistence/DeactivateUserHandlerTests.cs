using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Interceptors;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class DeactivateUserHandlerTests : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedOn = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly FakeTimeProvider _time = new(CreatedOn);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Desativar carimba UpdatedAt; repetir em quem já está inativo não deixa rastro")]
    public async Task HandleAsync_LeavesNoTrace_WhenRepeated()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new Repository<User>(context);
        var user = await new CreateUserCommandHandler(repository)
            .HandleAsync(new CreateUserCommand("Luiz Palma", Email.Create("norepeat@example.com")));

        var handler = new DeactivateUserCommandHandler(repository);

        // Act: primeira desativação, três horas depois da criação
        _time.Advance(TimeSpan.FromHours(3));
        await handler.HandleAsync(new DeactivateUserCommand(user.Id));

        var afterFirst = user.UpdatedAt;

        // Act: repetição, mais três horas adiante
        _time.Advance(TimeSpan.FromHours(3));
        await handler.HandleAsync(new DeactivateUserCommand(user.Id));

        // Assert
        Assert.Equal(CreatedOn.AddHours(3), afterFirst);

        await using var read = CreateContext();
        var persisted = await read.Users.SingleAsync(u => u.Id == user.Id);

        Assert.Equal(UserStatus.Inactive, persisted.Status);
        Assert.Equal(CreatedOn, persisted.CreatedAt);
        Assert.Equal(CreatedOn.AddHours(3), persisted.UpdatedAt);
    }

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditableEntityInterceptor(new StubCurrentUser(), _time))
            .Options;

        return new FinancariasDbContext(options);
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
    }
}
