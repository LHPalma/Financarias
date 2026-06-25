namespace Financarias.Application.Addresses;

/// <summary>
///     Porta de saída para consulta de endereço por CEP.
/// </summary>
public interface IAddressLookupProvider
{
    string ProviderName { get; }

    Task<AddressLookupResult?> FindAddressAsync(string cep, CancellationToken cancellationToken = default);
}