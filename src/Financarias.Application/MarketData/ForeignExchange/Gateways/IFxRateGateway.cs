using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.Gateways;

/// <summary>
///     Porta de saída para cotações de câmbio. Retorna a tabela de taxas relativa a uma moeda base.
///     Propaga exceção em falha de transporte.
/// </summary>
public interface IFxRateGateway
{
    Task<FxRateSnapshot> GetLatestRatesAsync(Currency baseCurrency, CancellationToken cancellationToken = default);
}