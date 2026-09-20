namespace Financarias.Application.Holidays.Import;

/// <summary>Resultado da importação de feriados.</summary>
/// <param name="TotalFetched">Quantos feriados o provedor devolveu.</param>
/// <param name="TotalSaved">Quantos eram novos (data e país ainda inexistentes) e foram gravados.</param>
public sealed record HolidayImportResult(int TotalFetched, int TotalSaved);