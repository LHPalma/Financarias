using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Indicators.Commands;
using Financarias.Application.MarketData.Indicators.DTOs.Requests;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Application.MarketData.Indicators.UseCases;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.Indicators;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Indicators.UseCases;

public class ImportIndicatorValuesUseCaseTests
{
    private static readonly IndicatorValueImportResult Result = new(194, 190, 4);

    private readonly ICommandHandler<ImportIndicatorValuesCommand, IndicatorValueImportResult> _handler =
        Substitute.For<ICommandHandler<ImportIndicatorValuesCommand, IndicatorValueImportResult>>();

    private readonly ImportIndicatorValuesUseCase _useCase;

    public ImportIndicatorValuesUseCaseTests()
    {
        _handler.HandleAsync(Arg.Any<ImportIndicatorValuesCommand>(), Arg.Any<CancellationToken>()).Returns(Result);
        _useCase = new ImportIndicatorValuesUseCase(_handler);
    }

    [Fact(DisplayName = "Um ano válido vira o comando e o resultado do handler é devolvido")]
    public async Task Execute_WithValidYear_ForwardsTheCommand()
    {
        // Act
        var result = await _useCase.ExecuteAsync(new ImportIndicatorValuesRequest(EconomicIndicator.Dollar, 2026));

        // Assert
        Assert.Same(Result, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<ImportIndicatorValuesCommand>(command => command == new ImportIndicatorValuesCommand(EconomicIndicator.Dollar, 2026)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "O primeiro ano aceito (1980) passa")]
    public async Task Execute_WithFirstYear_IsAccepted()
    {
        // Act
        await _useCase.ExecuteAsync(new ImportIndicatorValuesRequest(EconomicIndicator.Ipca, 1980));

        // Assert
        await _handler.Received(1).HandleAsync(Arg.Any<ImportIndicatorValuesCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "Ano anterior a 1980 lança erro de domínio sem chamar o handler")]
    [InlineData(1979)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Execute_WithYearBeforeFirst_ThrowsWithoutCallingTheHandler(int year)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _useCase.ExecuteAsync(new ImportIndicatorValuesRequest(EconomicIndicator.Cdi, year)));

        Assert.Equal("indicator.year.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportIndicatorValuesCommand>(), Arg.Any<CancellationToken>());
    }
}
