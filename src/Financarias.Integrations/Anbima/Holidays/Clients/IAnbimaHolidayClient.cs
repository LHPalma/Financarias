using Refit;

namespace Financarias.Integrations.Anbima.Holidays.Clients;

public interface IAnbimaHolidayClient
{
    [Get("/feriados/arqs/feriados_nacionais.xls")]
    Task<Stream> GetHolidaysFileAsync(CancellationToken cancellationToken = default);
}