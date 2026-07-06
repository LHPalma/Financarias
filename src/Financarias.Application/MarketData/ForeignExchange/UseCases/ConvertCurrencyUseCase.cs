using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Queries;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.UseCases;

public class ConvertCurrencyUseCase(
    IQueryHandler<ConvertCurrencyQuery, ConversionResult?> handler
) : IConvertCurrencyUseCase
{
    public Task<ConversionResult?> ExecuteAsync(
        decimal amount,
        Currency from,
        Currency to,
        int decimals = 2,
        CancellationToken cancellationToken = default)
        => handler.HandleAsync(new ConvertCurrencyQuery(amount, from, to, decimals), cancellationToken);
}