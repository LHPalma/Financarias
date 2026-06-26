using Financarias.Domain.Addresses;

namespace Financarias.Application.Addresses;

/// <summary>
/// Porta de saída para consulta de endereço por CEP. Retorna <c>null</c> quando o
/// CEP não é encontrado ou a consulta externa falha.
/// </summary>
public interface IAddressLookupProvider
{
    string ProviderName { get; }

    Task<AddressLookupResult?> FindAddressAsync(Cep cep, CancellationToken cancellationToken = default);
}