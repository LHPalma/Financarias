using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.Stocks.Commands;

public sealed record ImportDailyQuotesCommand(QuotePeriod Period) : ICommand<DailyQuoteImportResult>;
