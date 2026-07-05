using System.Net;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.Gateways;
using Financarias.Domain.MarketData;
using Financarias.Integrations.MarketData.Brapi.Clients;
using Financarias.Integrations.MarketData.Brapi.DTOs.Responses;
using Microsoft.Extensions.Logging;
using Refit;

namespace Financarias.Integrations.MarketData.Brapi.Providers;

public sealed class BrapiQuoteProvider(
    IBrapiClient client,
    ILogger<BrapiQuoteProvider> logger
) : IStockQuoteGateway
{
    public async Task<StockQuoteResult?> FindQuoteAsync(Ticker ticker, CancellationToken cancellationToken = default)
    {
        BrapiQuoteResponse response;

        try
        {
            response = await client.GetQuoteAsync(ticker.Value, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("Brapi 404 para {Ticker} — tratando como não encontrado", ticker);
            return null;
        }

        var data = response.Results?.FirstOrDefault();

        if (data?.RegularMarketPrice is null)
        {
            logger.LogWarning("Brapi sem preço para {Ticker}", ticker);
            return null;
        }

        return new StockQuoteResult(
            data.Symbol ?? ticker.Value,
            data.ShortName,
            data.Currency ?? "BRL",
            data.RegularMarketPrice.Value,
            data.RegularMarketChange,
            data.RegularMarketChangePercent,
            data.RegularMarketOpen,
            data.RegularMarketDayHigh,
            data.RegularMarketDayLow,
            data.RegularMarketPreviousClose,
            data.RegularMarketVolume,
            data.RegularMarketTime ?? DateTimeOffset.UtcNow
        );
    }
}