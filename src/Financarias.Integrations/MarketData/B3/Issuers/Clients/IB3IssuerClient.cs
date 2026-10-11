using Financarias.Integrations.MarketData.B3.Issuers.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.MarketData.B3.Issuers.Clients;

public interface IB3IssuerClient
{
    [Get("/listedCompaniesProxy/CompanyCall/GetInitialCompanies/{payload}")]
    Task<B3InitialCompaniesResponse> GetInitialCompaniesAsync(string payload, CancellationToken cancellationToken = default);

    [Get("/listedCompaniesProxy/CompanyCall/GetDetail/{payload}")]
    Task<B3CompanyDetail> GetDetailAsync(string payload, CancellationToken cancellationToken = default);
}
