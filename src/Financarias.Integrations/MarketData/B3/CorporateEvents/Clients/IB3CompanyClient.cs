using Financarias.Integrations.MarketData.B3.CorporateEvents.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.MarketData.B3.CorporateEvents.Clients;

public interface IB3CompanyClient
{
    [Get("/listedCompaniesProxy/CompanyCall/GetListedCashDividends/{payload}")]
    Task<B3CashDividendsResponse> GetCashDividendsAsync(string payload, CancellationToken cancellationToken = default);

    [Get("/listedCompaniesProxy/CompanyCall/GetListedSupplementCompany/{payload}")]
    Task<List<B3SupplementCompany>> GetSupplementAsync(string payload, CancellationToken cancellationToken = default);
}
