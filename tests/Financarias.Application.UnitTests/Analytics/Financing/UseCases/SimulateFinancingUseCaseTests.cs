using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Application.Analytics.Financing.Queries;
using Financarias.Application.Analytics.Financing.UseCases;
using Financarias.Application.Common.Messaging;
using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Exceptions;
using NSubstitute;

namespace Financarias.Application.UnitTests.Analytics.Financing.UseCases;

public class SimulateFinancingUseCaseTests
{
    private readonly IQueryHandler<SimulateFinancingQuery, FinancingSimulationResult> _handler =
        Substitute.For<IQueryHandler<SimulateFinancingQuery, FinancingSimulationResult>>();

    private readonly SimulateFinancingUseCase _useCase;

    public SimulateFinancingUseCaseTests() =>
        _useCase = new SimulateFinancingUseCase(new SimulateFinancingMapper(), _handler);

    [Fact(DisplayName = "Delega ao handler a query com os value objects montados pelo mapper")]
    public async Task ExecuteAsync_WithValidRequest_DelegatesMappedQuery()
    {
        // Arrange
        _handler.HandleAsync(Arg.Any<SimulateFinancingQuery>(), Arg.Any<CancellationToken>())
            .Returns(new FinancingSimulationResult(881.25m, 42300.03m, 12300.03m, []));

        // Act
        await _useCase.ExecuteAsync(new SimulateFinancingRequest(30000m, 0.015m, 48));

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<SimulateFinancingQuery>(query =>
                query.Principal == NominalValue.Create(30000m)
                && query.MonthlyRate == MonthlyRate.FromFraction(0.015m)
                && query.Installments == InstallmentCount.Create(48)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Entrada inválida lança no mapper e o handler nunca é chamado")]
    public async Task ExecuteAsync_WithInvalidRequest_ThrowsBeforeReachingHandler()
    {
        // Act & Assert
        Assert.Throws<InvalidMonthlyRateException>(() =>
        {
            _ = _useCase.ExecuteAsync(new SimulateFinancingRequest(30000m, -0.01m, 48));
        });

        await _handler.DidNotReceive().HandleAsync(Arg.Any<SimulateFinancingQuery>(), Arg.Any<CancellationToken>());
    }
}
