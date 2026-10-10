using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.Stocks.Import;

/// <summary>
/// Porta de origem das cotações diárias para importação: obtém, de um provedor externo (ex.: COTAHIST da B3),
/// o arquivo do período pedido. Arquivo inexistente (fim de semana, feriado, data futura) é um miss esperado
/// e devolve nulo; falha de transporte propaga.
/// </summary>
public interface IDailyQuoteProvider
{
    Task<IDailyQuoteFile?> OpenAsync(QuotePeriod period, CancellationToken cancellationToken = default);
}
