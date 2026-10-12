using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.SearchInterest.Import;

namespace Financarias.Application.MarketData.SearchInterest.Commands;

public sealed record ImportSearchInterestCommand(IReadOnlyList<string> Terms, string Geo, DateOnly From, DateOnly To)
    : ICommand<SearchInterestImportResult>;
