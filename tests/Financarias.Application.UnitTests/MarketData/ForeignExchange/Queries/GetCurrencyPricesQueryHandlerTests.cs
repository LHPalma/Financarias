using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Gateways;
using Financarias.Application.MarketData.ForeignExchange.Queries;
using Financarias.Domain.MarketData;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.ForeignExchange.Queries;

public class GetCurrencyPricesQueryHandlerTests
{
    private readonly IFxRateGateway _gateway = Substitute.For<IFxRateGateway>();

    private void RatesReturn(params (Currency Currency, decimal Rate)[] rates) =>
        _gateway.GetLatestRatesAsync(Arg.Any<Currency>(), Arg.Any<CancellationToken>())
            .Returns(new FxRateSnapshot(
                Currency.Brl,
                rates.ToDictionary(rate => rate.Currency, rate => rate.Rate),
                new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero)));

    [Fact(DisplayName = "Preço de cada moeda é o valor de 1 unidade em BRL, arredondado")]
    public async Task Handle_ReturnsPriceInBrl()
    {
        // Arrange: 1 BRL = 0,20 USD -> 1 USD = 5,00 BRL ; 1 BRL = 0,18 EUR -> 1 EUR = 5,5556 -> 2 casas = 5,56
        RatesReturn((Currency.Brl, 1m), (Currency.Usd, 0.20m), (Currency.Eur, 0.18m));
        var handler = new GetCurrencyPricesQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(
            new GetCurrencyPricesQuery([Currency.Usd, Currency.Eur], Currency.Brl, 2));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(Currency.Usd, result[0].Currency);
        Assert.Equal(Currency.Brl, result[0].Quote);
        Assert.Equal(5.00m, result[0].Price);
        Assert.Equal(5.56m, result[1].Price);
    }

    [Fact(DisplayName = "Cotação não-BRL triangula pelo pivô (preço do EUR em USD)")]
    public async Task Handle_WithNonBrlQuote_TriangulatesViaPivot()
    {
        // Arrange: 1 EUR = 5,5556 BRL e 1 USD = 5,00 BRL -> 1 EUR = 1,1111 USD -> 2 casas = 1,11
        RatesReturn((Currency.Brl, 1m), (Currency.Usd, 0.20m), (Currency.Eur, 0.18m));
        var handler = new GetCurrencyPricesQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(
            new GetCurrencyPricesQuery([Currency.Eur], Currency.Usd, 2));

        // Assert
        var price = Assert.Single(result);
        Assert.Equal(Currency.Usd, price.Quote);
        Assert.Equal(1.11m, price.Price);
    }

    [Fact(DisplayName = "Respeita o número de casas decimais do parâmetro")]
    public async Task Handle_RoundsToRequestedDecimals()
    {
        // Arrange: 1 BRL = 0,18 EUR -> 1 EUR = 5,5555... -> 4 casas = 5,5556
        RatesReturn((Currency.Brl, 1m), (Currency.Eur, 0.18m));
        var handler = new GetCurrencyPricesQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(
            new GetCurrencyPricesQuery([Currency.Eur], Currency.Brl, 4));

        // Assert
        Assert.Equal(5.5556m, result[0].Price);
    }

    [Fact(DisplayName = "Moeda fora da tabela é pulada (miss silencioso)")]
    public async Task Handle_UnknownCurrency_IsSkipped()
    {
        // Arrange: sem JPY na tabela
        RatesReturn((Currency.Brl, 1m), (Currency.Usd, 0.20m));
        var handler = new GetCurrencyPricesQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(
            new GetCurrencyPricesQuery([Currency.Usd, Currency.Jpy], Currency.Brl, 2));

        // Assert
        Assert.Equal(Currency.Usd, Assert.Single(result).Currency);
    }
}
