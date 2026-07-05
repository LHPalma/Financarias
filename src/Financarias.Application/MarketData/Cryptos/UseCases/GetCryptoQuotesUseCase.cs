using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.Queries;
using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Application.MarketData.Cryptos.UseCases;

public sealed class GetCryptoQuotesUseCase(
    IQueryHandler<GetCryptoQuotesQuery, IReadOnlyList<CryptoQuoteResult>> handler
) : IGetCryptoQuotesUseCase
{
    public Task<IReadOnlyList<CryptoQuoteResult>> ExecuteAsync(
        IReadOnlyList<CryptoAsset> assets,
        QuoteCurrency currency,
        CancellationToken cancellationToken = default)
        => handler.HandleAsync(new GetCryptoQuotesQuery(assets, currency), cancellationToken);
}