using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Application.MarketData.Cryptos.Gateways;

public interface ICryptoQuoteGateway
{
    Task<IReadOnlyList<CryptoQuoteResult>> FindQuotesAsync(
        IReadOnlyList<CryptoAsset> assets,
        QuoteCurrency currency,
        CancellationToken cancellationToken = default);
}