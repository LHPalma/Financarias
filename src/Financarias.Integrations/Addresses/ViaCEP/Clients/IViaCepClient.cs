using Financarias.Integrations.Addresses.ViaCep.DTOs.Responses;
using Refit;

namespace Financarias.Integrations.Addresses.ViaCep.Clients;

public interface IViaCepClient
{
    [Get("/{cep}/json")]
    Task<ViaCepResponse> FindAddressByCepAsync(string cep, CancellationToken cancellationToken = default);
}