using Financarias.Application.Common.Security;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class AuditableEntityInterceptorTests : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedOn = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext(null, new FakeTimeProvider(CreatedOn));
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Insert carimba os quatro campos com a hora e o usuário correntes")]
    public async Task SavingChanges_StampsEveryField_OnInsert()
    {
        // Arrange
        var author = Guid.CreateVersion7();
        var time = new FakeTimeProvider(CreatedOn);
        var user = User.Create("Inserido", Email.Create("insert@example.com"));

        // Act
        await using (var write = CreateContext(author, time))
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        // Assert
        await using var read = CreateContext(author, time);
        var found = await read.Users.SingleAsync(u => u.Id == user.Id);

        Assert.Equal(CreatedOn, found.CreatedAt);
        Assert.Equal(CreatedOn, found.UpdatedAt);
        Assert.Equal(author, found.CreatedBy);
        Assert.Equal(author, found.UpdatedBy);
    }

    [Fact(DisplayName = "Update mexe só nos campos de alteração e preserva os de criação")]
    public async Task SavingChanges_TouchesOnlyUpdateFields_OnModify()
    {
        // Arrange
        var creator = Guid.CreateVersion7();
        var editor = Guid.CreateVersion7();
        var time = new FakeTimeProvider(CreatedOn);
        var user = User.Create("Alterado", Email.Create("update@example.com"));

        await using (var write = CreateContext(creator, time))
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        time.Advance(TimeSpan.FromHours(3));

        // Act
        await using (var edit = CreateContext(editor, time))
        {
            var tracked = await edit.Users.SingleAsync(u => u.Id == user.Id);
            tracked.Deactivate();
            await edit.SaveChangesAsync();
        }

        // Assert
        await using var read = CreateContext(editor, time);
        var found = await read.Users.SingleAsync(u => u.Id == user.Id);

        Assert.Equal(CreatedOn, found.CreatedAt);
        Assert.Equal(creator, found.CreatedBy);
        Assert.Equal(CreatedOn.AddHours(3), found.UpdatedAt);
        Assert.Equal(editor, found.UpdatedBy);
    }

    [Fact(DisplayName = "Sem usuário corrente a autoria fica nula, sem quebrar a escrita")]
    public async Task SavingChanges_LeavesAuthorshipNull_WhenThereIsNoCurrentUser()
    {
        // Arrange: é o caso do import e de qualquer job, que escrevem sem requisição
        var time = new FakeTimeProvider(CreatedOn);
        var user = User.Create("Sem autor", Email.Create("noauthor@example.com"));

        // Act
        await using (var write = CreateContext(null, time))
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        // Assert
        await using var read = CreateContext(null, time);
        var found = await read.Users.SingleAsync(u => u.Id == user.Id);

        Assert.Equal(CreatedOn, found.CreatedAt);
        Assert.Null(found.CreatedBy);
        Assert.Null(found.UpdatedBy);
    }

    private FinancariasDbContext CreateContext(Guid? currentUserId, TimeProvider timeProvider)
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditableEntityInterceptor(new StubCurrentUser(currentUserId), timeProvider))
            .Options;

        return new FinancariasDbContext(options);
    }

    private sealed class StubCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;
    }
}
