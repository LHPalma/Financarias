using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Application.Analytics.Financing.Queries;
using Financarias.Application.Analytics.Financing.UseCases;
using Financarias.Application.Common.Messaging;
using Financarias.Domain.Analytics;
using Financarias.Domain.Common.Exceptions;
using NSubstitute;

namespace Financarias.Application.UnitTests.Analytics.Financing.UseCases;

public class SimulateEarlyPayoffUseCaseTests
{
    private readonly IQueryHandler<SimulateEarlyPayoffQuery, EarlyPayoffResult> _handler =
        Substitute.For<IQueryHandler<SimulateEarlyPayoffQuery, EarlyPayoffResult>>();

    private readonly SimulateEarlyPayoffUseCase _useCase;

    public SimulateEarlyPayoffUseCaseTests() =>
        _useCase = new SimulateEarlyPayoffUseCase(new SimulateEarlyPayoffMapper(), _handler);

    [Fact(DisplayName = "Delega ao handler a query com os value objects e a parcela alvo")]
    public async Task ExecuteAsync_WithValidRequest_DelegatesMappedQuery()
    {
        // Arrange
        _handler.HandleAsync(Arg.Any<SimulateEarlyPayoffQuery>(), Arg.Any<CancellationToken>())
            .Returns(new EarlyPayoffResult(5, 15558.04m, 5, 1823.19m, 707.06m));

        // Act
        await _useCase.ExecuteAsync(new SimulateEarlyPayoffRequest(30000m, 0.015m, 10, 5));

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<SimulateEarlyPayoffQuery>(query =>
                query.Principal == NominalValue.Create(30000m)
                && query.MonthlyRate == MonthlyRate.FromFraction(0.015m)
                && query.Installments == InstallmentCount.Create(10)
                && query.AtInstallment == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Entrada inválida lança no mapper e o handler nunca é chamado")]
    public async Task ExecuteAsync_WithInvalidRequest_ThrowsBeforeReachingHandler()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
        {
            _ = _useCase.ExecuteAsync(new SimulateEarlyPayoffRequest(30000m, 0.015m, 0, 5));
        });

        Assert.Equal("analytics.installmentcount.invalid", exception.Code);

        await _handler.DidNotReceive().HandleAsync(Arg.Any<SimulateEarlyPayoffQuery>(), Arg.Any<CancellationToken>());
    }
}
