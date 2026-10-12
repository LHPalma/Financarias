namespace Financarias.Application.MarketData.IndexComposition.Import;

/// <summary>Resultado da importação das carteiras teóricas históricas dos índices.</summary>
/// <param name="FilesRead">Arquivos lidos até o fim, tenham ou não trazido carteira.</param>
/// <param name="Portfolios">Carteiras (uma por índice, período e tipo) encontradas nos arquivos lidos. Um arquivo só de entradas e saídas não conta nenhuma.</param>
/// <param name="Rows">Linhas de composição (um ativo em uma carteira) lidas e processadas.</param>
/// <param name="Created">Linhas novas gravadas.</param>
/// <param name="Updated">Linhas que já existiam (mesmo índice, período, tipo e ativo) e mudaram, atualizadas.</param>
/// <param name="Failures">Arquivos que não puderam ser importados, com o motivo. Um arquivo com falha não impede os demais.</param>
public sealed record IndexCompositionImportResult(
    int FilesRead,
    int Portfolios,
    int Rows,
    int Created,
    int Updated,
    IReadOnlyList<IndexCompositionImportFailure> Failures);

/// <summary>Arquivo que falhou na importação.</summary>
/// <param name="Url">Endereço do arquivo.</param>
/// <param name="Reason">Motivo da falha.</param>
public sealed record IndexCompositionImportFailure(
    string Url,
    string Reason);
