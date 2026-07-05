using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.Gateways;

namespace Financarias.Application.MarketData.Cryptos.Queries;

public sealed class GetCryptoQuotesQueryHandler(
    ICryptoQuoteGateway gateway
) : IQueryHandler<GetCryptoQuotesQuery, IReadOnlyList<CryptoQuoteResult>>
{
    public Task<IReadOnlyList<CryptoQuoteResult>> HandleAsync(
        GetCryptoQuotesQuery query,
        CancellationToken cancellationToken = default) =>
        gateway.FindQuotesAsync(query.Assets, query.Currency, cancellationToken);
}