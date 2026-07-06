using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Queries;
using Financarias.Application.MarketData.ForeignExchange.UseCases;
using Financarias.Domain.MarketData;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.ForeignExchange.UseCases;

public class GetCurrencyPricesUseCaseTests
{
    private readonly IQueryHandler<GetCurrencyPricesQuery, IReadOnlyList<CurrencyPriceResult>> _handler =
        Substitute.For<IQueryHandler<GetCurrencyPricesQuery, IReadOnlyList<CurrencyPriceResult>>>();

    [Fact(DisplayName = "Monta a query com as moedas/casas e delega ao handler")]
    public async Task Execute_DelegatesToHandler()
    {
        // Arrange
        var expected = new List<CurrencyPriceResult>();
        _handler.HandleAsync(Arg.Any<GetCurrencyPricesQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
        var useCase = new GetCurrencyPricesUseCase(_handler);

        // Act
        var result = await useCase.ExecuteAsync([Currency.Usd, Currency.Eur], Currency.Usd, 3);

        // Assert
        Assert.Same(expected, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<GetCurrencyPricesQuery>(q =>
                q.Currencies.Contains(Currency.Usd) && q.Currencies.Contains(Currency.Eur)
                && q.Quote == Currency.Usd && q.Decimals == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Cotação default BRL e casas default 2 quando omitidas")]
    public async Task Execute_DefaultsQuoteToBrlAndDecimalsToTwo()
    {
        // Arrange
        _handler.HandleAsync(Arg.Any<GetCurrencyPricesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<CurrencyPriceResult>());
        var useCase = new GetCurrencyPricesUseCase(_handler);

        // Act
        await useCase.ExecuteAsync([Currency.Usd]);

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<GetCurrencyPricesQuery>(q => q.Quote == Currency.Brl && q.Decimals == 2),
            Arg.Any<CancellationToken>());
    }
}
