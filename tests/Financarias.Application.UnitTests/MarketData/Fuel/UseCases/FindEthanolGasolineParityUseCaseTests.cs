using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Application.MarketData.Fuel.UseCases;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Fuel.UseCases;

public class FindEthanolGasolineParityUseCaseTests
{
    private readonly IQueryHandler<FindEthanolGasolineParityQuery, IQueryable<EthanolGasolineParityResult>> _handler =
        Substitute.For<IQueryHandler<FindEthanolGasolineParityQuery, IQueryable<EthanolGasolineParityResult>>>();

    [Fact(DisplayName = "Executa a query de paridade e devolve o resultado do handler")]
    public async Task Execute_DelegatesToHandler()
    {
        // Arrange
        IQueryable<EthanolGasolineParityResult> expected =
            new[] { new EthanolGasolineParityResult("Posto Copacabana", "IPIRANGA", "CRUZEIRO DO SUL", "AC", 3.50m, 5.00m, 0.70m, false) }
                .AsQueryable();
        _handler.HandleAsync(Arg.Any<FindEthanolGasolineParityQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
        var useCase = new FindEthanolGasolineParityUseCase(_handler);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Same(expected, result);
        await _handler.Received(1).HandleAsync(
            Arg.Any<FindEthanolGasolineParityQuery>(), Arg.Any<CancellationToken>());
    }
}
