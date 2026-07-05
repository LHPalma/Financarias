using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.Queries;
using Financarias.Application.MarketData.Stocks.UseCases;
using Financarias.Domain.MarketData;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Stocks.UseCases;

public class GetStockQuoteUseCaseTests
{
    private readonly IQueryHandler<GetStockQuoteQuery, StockQuoteResult?> _handler;
    private readonly GetStockQuoteUseCase _useCase;

    public GetStockQuoteUseCaseTests()
    {
        _handler = Substitute.For<IQueryHandler<GetStockQuoteQuery, StockQuoteResult?>>();
        _useCase = new GetStockQuoteUseCase(_handler);
    }

    [Fact(DisplayName = "Valida o ticker, delega ao handler e retorna a cotação")]
    public async Task Execute_WithValidTicker_DelegatesToHandlerAndReturnsQuote()
    {
        // Arrange
        var expected = new StockQuoteResult(
            "PETR4", "PETROBRAS PN", "BRL", 38.25m, 0.29m, 0.76m,
            38.07m, 38.25m, 37.86m, 38.24m, 10359300L, DateTimeOffset.UtcNow);
        _handler
            .HandleAsync(new GetStockQuoteQuery(Ticker.Create("PETR4")), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _useCase.ExecuteAsync("petr4");

        // Assert
        Assert.Same(expected, result);
    }

    [Fact(DisplayName = "Lança InvalidTickerException e não chama o handler para ticker inválido")]
    public async Task Execute_WithInvalidTicker_ThrowsAndDoesNotCallHandler()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidTickerException>(() => _useCase.ExecuteAsync("PETR"));
        await _handler.DidNotReceive().HandleAsync(Arg.Any<GetStockQuoteQuery>(), Arg.Any<CancellationToken>());
    }
}
