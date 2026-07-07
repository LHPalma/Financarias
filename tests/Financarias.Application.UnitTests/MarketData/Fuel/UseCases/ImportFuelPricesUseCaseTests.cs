using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Fuel.Commands;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Fuel.UseCases;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Fuel.UseCases;

public class ImportFuelPricesUseCaseTests
{
    private readonly ICommandHandler<ImportFuelPricesCommand, FuelImportResult> _handler =
        Substitute.For<ICommandHandler<ImportFuelPricesCommand, FuelImportResult>>();

    [Fact(DisplayName = "Executa o comando de import e devolve o resultado do handler")]
    public async Task Execute_DelegatesToHandler()
    {
        // Arrange
        var expected = new FuelImportResult(10, 3, 7, 2);
        _handler.HandleAsync(Arg.Any<ImportFuelPricesCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var useCase = new ImportFuelPricesUseCase(_handler);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Same(expected, result);
        await _handler.Received(1).HandleAsync(Arg.Any<ImportFuelPricesCommand>(), Arg.Any<CancellationToken>());
    }
}
