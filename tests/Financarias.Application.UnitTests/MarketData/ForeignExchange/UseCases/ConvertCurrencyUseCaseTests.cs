using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Queries;
using Financarias.Application.MarketData.ForeignExchange.UseCases;
using Financarias.Domain.MarketData;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.ForeignExchange.UseCases;

public class ConvertCurrencyUseCaseTests
{
    private readonly IQueryHandler<ConvertCurrencyQuery, ConversionResult?> _handler =
        Substitute.For<IQueryHandler<ConvertCurrencyQuery, ConversionResult?>>();

    [Fact(DisplayName = "Monta a query com valor/moedas/casas e delega ao handler")]
    public async Task Execute_DelegatesToHandler()
    {
        // Arrange
        var expected = new ConversionResult(Currency.Usd, Currency.Brl, 10m, 51.8m, 5.18m,
            new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero));
        _handler.HandleAsync(Arg.Any<ConvertCurrencyQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
        var useCase = new ConvertCurrencyUseCase(_handler);

        // Act
        var result = await useCase.ExecuteAsync(10m, Currency.Usd, Currency.Brl, 3);

        // Assert
        Assert.Same(expected, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<ConvertCurrencyQuery>(q =>
                q.Amount == 10m && q.From == Currency.Usd && q.To == Currency.Brl && q.Decimals == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Casas decimais têm default 2 quando omitidas")]
    public async Task Execute_DefaultsDecimalsToTwo()
    {
        // Arrange
        _handler.HandleAsync(Arg.Any<ConvertCurrencyQuery>(), Arg.Any<CancellationToken>())
            .Returns((ConversionResult?)null);
        var useCase = new ConvertCurrencyUseCase(_handler);

        // Act
        await useCase.ExecuteAsync(10m, Currency.Usd, Currency.Brl);

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<ConvertCurrencyQuery>(q => q.Decimals == 2),
            Arg.Any<CancellationToken>());
    }
}
