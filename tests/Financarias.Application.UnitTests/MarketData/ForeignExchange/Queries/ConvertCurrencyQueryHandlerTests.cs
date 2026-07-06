using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Gateways;
using Financarias.Application.MarketData.ForeignExchange.Queries;
using Financarias.Domain.MarketData;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.ForeignExchange.Queries;

public class ConvertCurrencyQueryHandlerTests
{
    private readonly IFxRateGateway _gateway = Substitute.For<IFxRateGateway>();

    private void RatesReturn(params (Currency Currency, decimal Rate)[] rates) =>
        _gateway.GetLatestRatesAsync(Arg.Any<Currency>(), Arg.Any<CancellationToken>())
            .Returns(new FxRateSnapshot(
                Currency.Brl,
                rates.ToDictionary(rate => rate.Currency, rate => rate.Rate),
                new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero)));

    [Fact(DisplayName = "Converte via BRL e devolve o valor arredondado")]
    public async Task Handle_Converts_ViaBrl()
    {
        // Arrange: 1 BRL = 0,20 USD e 0,18 EUR -> 100 USD = 90 EUR
        RatesReturn((Currency.Brl, 1m), (Currency.Usd, 0.20m), (Currency.Eur, 0.18m));
        var handler = new ConvertCurrencyQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(new ConvertCurrencyQuery(100m, Currency.Usd, Currency.Eur, 2));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(90m, result.ConvertedAmount);
    }

    [Fact(DisplayName = "Respeita o número de casas decimais do parâmetro")]
    public async Task Handle_RoundsToRequestedDecimals()
    {
        // Arrange: 100 USD -> 100 * 0,20/0,18 = 111,111... -> 4 casas
        RatesReturn((Currency.Brl, 1m), (Currency.Usd, 0.18m), (Currency.Eur, 0.20m));
        var handler = new ConvertCurrencyQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(new ConvertCurrencyQuery(100m, Currency.Usd, Currency.Eur, 4));

        // Assert
        Assert.Equal(111.1111m, result!.ConvertedAmount);
    }

    [Fact(DisplayName = "Moeda ausente da tabela retorna null (miss)")]
    public async Task Handle_UnknownCurrency_ReturnsNull()
    {
        // Arrange: sem JPY na tabela
        RatesReturn((Currency.Brl, 1m), (Currency.Usd, 0.20m));
        var handler = new ConvertCurrencyQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(new ConvertCurrencyQuery(10m, Currency.Usd, Currency.Jpy, 2));

        // Assert
        Assert.Null(result);
    }
}