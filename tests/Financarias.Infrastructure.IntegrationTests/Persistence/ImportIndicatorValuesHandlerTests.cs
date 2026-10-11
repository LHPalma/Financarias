using Financarias.Application.MarketData.Indicators.Commands;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class ImportIndicatorValuesHandlerTests : IAsyncLifetime
{
    private static readonly DateOnly Day1 = new(2026, 10, 8);
    private static readonly DateOnly Day2 = new(2026, 10, 9);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Import insere os valores e faz upsert real na reimportação (insere, atualiza e pula)")]
    public async Task Import_PersistsThenUpserts_AgainstRealPostgres()
    {
        // Arrange & Act — primeira importação
        var first = await RunImportAsync(EconomicIndicator.Dollar, 2026, Item(Day1, 5.0119m));

        // Assert
        Assert.Equal(new IndicatorValueImportResult(1, 1, 0), first);

        // Act — reimportação: o dia 1 mudou e chega o dia 2
        var second = await RunImportAsync(EconomicIndicator.Dollar, 2026, Item(Day1, 5.0120m), Item(Day2, 4.9892m));

        // Assert
        Assert.Equal(new IndicatorValueImportResult(2, 1, 1), second);

        // Act — terceira rodada idêntica: nada muda
        var third = await RunImportAsync(EconomicIndicator.Dollar, 2026, Item(Day1, 5.0120m), Item(Day2, 4.9892m));

        // Assert
        Assert.Equal(new IndicatorValueImportResult(2, 0, 0), third);

        await using var verify = CreateContext();
        Assert.Equal(2, await verify.IndicatorValues.CountAsync());
        Assert.Equal(5.0120m, (await verify.IndicatorValues.SingleAsync(v => v.ReferenceDate == Day1)).Value);
    }

    [Fact(DisplayName = "A specification não mistura anos nem indicadores")]
    public async Task Import_DoesNotTouchOtherYearsOrIndicators()
    {
        // Arrange — um dólar de 2025 e uma Selic de 2026 já existem
        await RunImportAsync(EconomicIndicator.Dollar, 2025, Item(new DateOnly(2025, 10, 8), 5.4000m));
        await RunImportAsync(EconomicIndicator.SelicTarget, 2026, Item(Day1, 15.00m, EconomicIndicator.SelicTarget));

        // Act — importa o dólar de 2026
        var result = await RunImportAsync(EconomicIndicator.Dollar, 2026, Item(Day1, 5.0119m));

        // Assert
        Assert.Equal(new IndicatorValueImportResult(1, 1, 0), result);

        await using var verify = CreateContext();
        Assert.Equal(3, await verify.IndicatorValues.CountAsync());
    }

    private static IndicatorValueImportItem Item(
        DateOnly referenceDate,
        decimal value,
        EconomicIndicator indicator = EconomicIndicator.Dollar) =>
        new(indicator, referenceDate, value);

    private async Task<IndicatorValueImportResult> RunImportAsync(
        EconomicIndicator indicator,
        int year,
        params IndicatorValueImportItem[] items)
    {
        await using var context = CreateContext();
        var handler = new ImportIndicatorValuesCommandHandler(new StubProvider(items), new Repository<IndicatorValue>(context));

        return await handler.HandleAsync(new ImportIndicatorValuesCommand(indicator, year));
    }

    private sealed class StubProvider(IReadOnlyList<IndicatorValueImportItem> items) : IIndicatorValueProvider
    {
        public Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
            EconomicIndicator indicator,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(items);
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
