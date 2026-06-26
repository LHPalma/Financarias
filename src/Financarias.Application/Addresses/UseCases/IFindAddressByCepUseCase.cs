namespace Financarias.Application.Addresses.UseCases;

/// <summary>
/// Caso de uso de consulta de endereço por CEP: valida a entrada crua e orquestra a busca.
/// </summary>
public interface IFindAddressByCepUseCase
{
    Task<AddressLookupResult?> ExecuteAsync(string cep, CancellationToken cancellationToken = default);
}
