using Financarias.Application.Addresses;
using Financarias.Integrations.Addresses.ViaCep.Clients;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.Addresses.ViaCep.Providers;

public sealed class ViaCepProvider(
    IViaCepClient client,
    ILogger<ViaCepProvider> logger
) : IAddressLookupProvider
{
    public string ProviderName => "ViaCEP";

    public async Task<AddressLookupResult?> FindAddressAsync(string cep, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.FindAddressByCepAsync(cep, cancellationToken);

            if (response is null || response.Erro == true)
            {
                logger.LogWarning($"Cep {cep} not found");
                return null;
            }

            return new AddressLookupResult(
                response.Logradouro,
                response.Bairro,
                response.Localidade,
                response.Uf,
                response.Complemento
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while looking up address with ViaCep: {cep}");
            return null;
        }
    }
}