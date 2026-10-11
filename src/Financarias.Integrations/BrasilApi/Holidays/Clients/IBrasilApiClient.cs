using Financarias.Integrations.BrasilApi.Holidays.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.BrasilApi.Holidays.Clients;

public interface IBrasilApiClient
{
    [Get("/api/feriados/v1/{year}")]
    Task<List<BrasilApiHoliday>> GetHolidaysAsync(int year, CancellationToken cancellationToken = default);
}
