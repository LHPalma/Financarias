using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Indicators.Commands;
using Financarias.Application.MarketData.Indicators.DTOs.Requests;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.UseCases;

public class ImportIndicatorValuesUseCase(
    ICommandHandler<ImportIndicatorValuesCommand, IndicatorValueImportResult> handler
) : IImportIndicatorValuesUseCase
{
    public Task<IndicatorValueImportResult> ExecuteAsync(
        ImportIndicatorValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Year < IndicatorValue.FirstYear)
        {
            throw MarketDataErrors.IndicatorYear(request.Year);
        }

        return handler.HandleAsync(new ImportIndicatorValuesCommand(request.Indicator, request.Year), cancellationToken);
    }
}
