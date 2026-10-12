using Financarias.Application.MarketData.SearchInterest.DTOs.Requests;
using Financarias.Application.MarketData.SearchInterest.Import;

namespace Financarias.Application.MarketData.SearchInterest.UseCases;

public interface IImportSearchInterestUseCase
{
    /// <summary>
    /// Importa o interesse de busca diário de um grupo de termos (1 a 5) em um país, num intervalo de até 270
    /// dias, e grava só o que é novo ou mudou. Lança erro de domínio quando o grupo, o país ou o intervalo é inválido.
    /// </summary>
    Task<SearchInterestImportResult> ExecuteAsync(ImportSearchInterestRequest request, CancellationToken cancellationToken = default);
}
