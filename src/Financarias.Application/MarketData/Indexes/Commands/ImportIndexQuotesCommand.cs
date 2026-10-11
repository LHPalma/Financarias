using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Domain.MarketData.Indexes;

namespace Financarias.Application.MarketData.Indexes.Commands;

public sealed record ImportIndexQuotesCommand(MarketIndex MarketIndex, int Year) : ICommand<IndexQuoteImportResult>;
