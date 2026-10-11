using Financarias.Application.MarketData.Issuers.Import;

namespace Financarias.Application.MarketData.Issuers.UseCases;

public interface IImportIssuersUseCase
{
    /// <summary>
    /// Cadastra, a partir da B3, os emissores que têm cotações já importadas: dados da empresa e classificação
    /// setorial. Grava só o que é novo ou mudou; emissor que a B3 não lista fica de fora.
    /// </summary>
    Task<IssuerImportResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
