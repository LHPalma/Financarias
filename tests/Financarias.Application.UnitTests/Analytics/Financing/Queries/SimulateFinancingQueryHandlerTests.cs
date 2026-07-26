using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Application.Analytics.Financing.Queries;
using Financarias.Domain.Analytics;

namespace Financarias.Application.UnitTests.Analytics.Financing.Queries;

public class SimulateFinancingQueryHandlerTests
{
    private readonly SimulateFinancingQueryHandler _handler = new(new SimulateFinancingMapper());

    private static SimulateFinancingQuery Query(decimal principal, decimal monthlyRate, int installments) =>
        new(NominalValue.Create(principal), MonthlyRate.FromFraction(monthlyRate), InstallmentCount.Create(installments));

    [Fact(DisplayName = "Devolve a parcela, os agregados e a tabela completa")]
    public async Task HandleAsync_WithValidQuery_ReturnsInstallmentAndSchedule()
    {
        // Arrange — 30.000 a 1,5% a.m. em 48 parcelas
        var query = Query(30000m, 0.015m, 48);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(881.25m, result.Installment);
        Assert.Equal(42300.03m, result.TotalPaid);
        Assert.Equal(12300.03m, result.TotalInterest);
        Assert.Equal(48, result.Schedule.Count);
    }

    [Fact(DisplayName = "Cada linha da tabela preserva período, juros, amortização e saldo")]
    public async Task HandleAsync_WithValidQuery_MapsEveryScheduleRow()
    {
        // Arrange
        var query = Query(30000m, 0.015m, 48);

        // Act
        var schedule = (await _handler.HandleAsync(query)).Schedule;

        // Assert
        Assert.Equal(1, schedule[0].Period);
        Assert.Equal(881.25m, schedule[0].Installment);
        Assert.Equal(450.00m, schedule[0].Interest);
        Assert.Equal(450.00m, schedule[0].AccumulatedInterest);
        Assert.Equal(431.25m, schedule[0].Amortization);
        Assert.Equal(29568.75m, schedule[0].OutstandingBalance);
        Assert.Equal(48, schedule[^1].Period);
        Assert.Equal(881.28m, schedule[^1].Installment);
        Assert.Equal(12300.03m, schedule[^1].AccumulatedInterest);
        Assert.Equal(0m, schedule[^1].OutstandingBalance);
    }

    [Fact(DisplayName = "Taxa zero devolve tabela sem juros")]
    public async Task HandleAsync_WithZeroRate_ReturnsScheduleWithoutInterest()
    {
        // Arrange
        var query = Query(10000m, 0m, 10);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1000m, result.Installment);
        Assert.Equal(0m, result.TotalInterest);
        Assert.All(result.Schedule, row => Assert.Equal(0m, row.Interest));
    }
}
