using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.IndexComposition.Import;

namespace Financarias.Application.MarketData.IndexComposition.Commands;

public sealed record ImportIndexCompositionFileCommand(string SourceUrl) : ICommand<IndexCompositionFileImportResult>;
