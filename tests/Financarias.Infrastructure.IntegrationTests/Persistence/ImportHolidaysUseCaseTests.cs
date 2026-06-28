using Financarias.Application.Holidays.Import;
using Financarias.Application.Holidays.UseCases;
using Financarias.Domain.Holidays.Models;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class ImportHolidaysUseCaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }

    private sealed class StubHolidayProvider(IReadOnlyList<HolidayImportItem> items) : IHolidayProvider
    {
        public Task<IReadOnlyList<HolidayImportItem>> FetchHolidaysAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(items);
    }

    [Fact(DisplayName = "Importa só os feriados novos (dedup contra o banco e dentro do arquivo)")]
    public async Task Import_PersistsOnlyNewHolidays_SkippingExistingAndIntraFileDuplicates()
    {
        // Arrange — Natal/2024 já está no banco
        await using (var seed = CreateContext())
        {
            var repository = new Repository<Holiday>(seed);
            await repository.AddAsync(Holiday.Create(new DateOnly(2024, 12, 25), "Natal", CountryCode.BR));
        }

        var provider = new StubHolidayProvider(
        [
            new HolidayImportItem(new DateOnly(2024, 12, 25), "Natal", CountryCode.BR),                  // já existe -> pula
            new HolidayImportItem(new DateOnly(2025, 1, 1), "Confraternização", CountryCode.BR),         // novo
            new HolidayImportItem(new DateOnly(2025, 1, 1), "Duplicado no arquivo", CountryCode.BR),     // dup intra-arquivo -> pula
            new HolidayImportItem(new DateOnly(2025, 4, 21), "Tiradentes", CountryCode.BR),              // novo
        ]);

        await using var context = CreateContext();
        var useCase = new ImportHolidaysUseCase(provider, context, new Repository<Holiday>(context));

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Equal(4, result.TotalFetched);
        Assert.Equal(2, result.TotalSaved);

        await using var verify = CreateContext();
        var persisted = await verify.Holidays.CountAsync();
        Assert.Equal(3, persisted); // Natal pré-existente + os 2 novos
    }
}
