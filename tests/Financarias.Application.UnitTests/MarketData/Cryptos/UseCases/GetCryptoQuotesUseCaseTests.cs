using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.Queries;
using Financarias.Application.MarketData.Cryptos.UseCases;
using Financarias.Domain.MarketData.Cryptos;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Cryptos.UseCases;

public class GetCryptoQuotesUseCaseTests
{
    [Fact(DisplayName = "Monta a query com os ativos/moeda e delega ao handler")]
    public async Task Execute_DelegatesToHandlerAndReturnsQuotes()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<GetCryptoQuotesQuery, IReadOnlyList<CryptoQuoteResult>>>();
        var expected = new List<CryptoQuoteResult>();
        handler.HandleAsync(Arg.Any<GetCryptoQuotesQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
        var useCase = new GetCryptoQuotesUseCase(handler);

        // Act
        var result = await useCase.ExecuteAsync([CryptoAsset.Bitcoin], QuoteCurrency.Brl);

        // Assert
        Assert.Same(expected, result);
        await handler.Received(1).HandleAsync(
            Arg.Is<GetCryptoQuotesQuery>(q => q.Assets.Contains(CryptoAsset.Bitcoin) && q.Currency == QuoteCurrency.Brl),
            Arg.Any<CancellationToken>());
    }
}
