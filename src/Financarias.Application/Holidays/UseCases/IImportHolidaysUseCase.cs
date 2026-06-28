using Financarias.Application.Holidays.Import;

namespace Financarias.Application.Holidays.UseCases;

/// <summary>
/// Importa os feriados do provedor externo e persiste apenas os ainda inexistentes
/// (dedup por data + país), devolvendo quantos foram buscados e quantos salvos.
/// </summary>
public interface IImportHolidaysUseCase
{
    Task<HolidayImportResult> ExecuteAsync(CancellationToken cancellationToken = default);
}