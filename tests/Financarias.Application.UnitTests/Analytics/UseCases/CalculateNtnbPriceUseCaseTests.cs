using Financarias.Application.Analytics.DTOs.Requests;
using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Analytics.Mappers;
using Financarias.Application.Analytics.Queries;
using Financarias.Application.Analytics.UseCases;
using Financarias.Application.Common.Messaging;
using NSubstitute;

namespace Financarias.Application.UnitTests.Analytics.UseCases;

public class CalculateNtnbPriceUseCaseTests
{
    [Fact(DisplayName = "Mapeia o request em query (com VOs) e delega ao handler")]
    public async Task Execute_MapsRequestAndDelegatesToHandler()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<CalculateNtnbPriceQuery, NtnbPriceResult>>();
        var expected = new NtnbPriceResult(new DateOnly(2024, 5, 23), 4321.5m, 2700, 1234.567890m);
        handler.HandleAsync(Arg.Any<CalculateNtnbPriceQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var useCase = new CalculateNtnbPriceUseCase(new CalculateNtnbPriceMapper(), handler);
        var request = new CalculateNtnbPriceRequest(4300m, 0.07m, 0.0046m, new DateOnly(2024, 5, 21), new DateOnly(2035, 5, 15));

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        Assert.Same(expected, result);
        await handler.Received(1).HandleAsync(
            Arg.Is<CalculateNtnbPriceQuery>(q =>
                q.TradeDate == request.TradeDate
                && q.DueDate == request.DueDate
                && q.Inflation == request.Inflation),
            Arg.Any<CancellationToken>());
    }
}
