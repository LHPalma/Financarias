using Financarias.Domain.Holidays.Models;

namespace Financarias.Application.Holidays.UseCases;

/// <summary>
/// Conta os dias úteis no intervalo (start, end], descontando fins de semana e os feriados
/// do país persistidos. Carrega os feriados do range e os aplica ao calendário de dias úteis.
/// </summary>
public interface ICountBusinessDaysUseCase
{
    Task<int> ExecuteAsync(
        DateOnly start,
        DateOnly end,
        CountryCode countryCode,
        CancellationToken cancellationToken = default);
}