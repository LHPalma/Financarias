using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.UseCases;

/// <summary>
///     Converte um valor entre duas moedas, com o número de casas decimais definido pelo chamador (default 2).
/// </summary>
public interface IConvertCurrencyUseCase
{
    Task<ConversionResult?> ExecuteAsync(
        decimal amount,
        Currency from,
        Currency to,
        int decimals = 2,
        CancellationToken cancellationToken = default);
}