using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Issuers.Import;

namespace Financarias.Application.MarketData.Issuers.Commands;

public sealed record ImportIssuersCommand(IReadOnlyCollection<string> IssuerCodes) : ICommand<IssuerImportResult>;
