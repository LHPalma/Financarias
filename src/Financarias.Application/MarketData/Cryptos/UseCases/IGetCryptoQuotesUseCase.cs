using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Application.MarketData.Cryptos.UseCases;

/// <summary>
///     Caso de uso de consulta de cotações de criptoativos por lista de ativos e moeda.
/// </summary>
public interface IGetCryptoQuotesUseCase
{
    Task<IReadOnlyList<CryptoQuoteResult>> ExecuteAsync(
        IReadOnlyList<CryptoAsset> assets,
        QuoteCurrency currency,
        CancellationToken cancellationToken = default);
}