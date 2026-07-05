using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Application.MarketData.Cryptos.Queries;

public sealed record GetCryptoQuotesQuery(
    IReadOnlyList<CryptoAsset> Assets,
    QuoteCurrency Currency
) : IQuery<IReadOnlyList<CryptoQuoteResult>>;