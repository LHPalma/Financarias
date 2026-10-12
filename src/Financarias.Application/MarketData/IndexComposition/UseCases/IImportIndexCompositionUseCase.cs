using Financarias.Application.MarketData.IndexComposition.DTOs.Requests;
using Financarias.Application.MarketData.IndexComposition.Import;

namespace Financarias.Application.MarketData.IndexComposition.UseCases;

public interface IImportIndexCompositionUseCase
{
    /// <summary>
    /// Importa as carteiras teóricas dos índices contidas em arquivos publicados pela B3 e persiste por upsert.
    /// Lança quando algum endereço não é um arquivo de dados da B3; um arquivo que falha na leitura entra no
    /// resultado e não impede os demais.
    /// </summary>
    Task<IndexCompositionImportResult> ExecuteAsync(
        ImportIndexCompositionRequest request,
        CancellationToken cancellationToken = default);
}
