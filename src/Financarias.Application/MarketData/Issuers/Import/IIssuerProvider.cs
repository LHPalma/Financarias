namespace Financarias.Application.MarketData.Issuers.Import;

/// <summary>
/// Porta de origem do cadastro dos emissores: obtém, de um provedor externo (ex.: empresas listadas da B3),
/// os dados cadastrais e a classificação setorial dos emissores pedidos. Emissor que o provedor não lista
/// fica fora do resultado e é contado em <see cref="IssuerFetch.NotListed"/>; falha de transporte propaga.
/// </summary>
public interface IIssuerProvider
{
    Task<IssuerFetch> GetAsync(IReadOnlyCollection<string> issuerCodes, CancellationToken cancellationToken = default);
}

/// <summary>Cadastros lidos e quantos emissores pedidos o provedor não lista.</summary>
public sealed record IssuerFetch(IReadOnlyList<IssuerImportItem> Items, int NotListed);
