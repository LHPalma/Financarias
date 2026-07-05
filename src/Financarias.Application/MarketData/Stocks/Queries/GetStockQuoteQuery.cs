using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.Stocks.Queries;

public sealed record GetStockQuoteQuery(Ticker Ticker)
    : IQuery<StockQuoteResult?>;