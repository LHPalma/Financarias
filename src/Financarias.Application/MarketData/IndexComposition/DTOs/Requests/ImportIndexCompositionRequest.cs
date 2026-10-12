namespace Financarias.Application.MarketData.IndexComposition.DTOs.Requests;

/// <summary>
/// Arquivos de carteira teórica a importar.
/// </summary>
/// <param name="Urls">Endereços dos arquivos (planilha ou ZIP) publicados pela B3, no formato `https://www.b3.com.br/data/files/...`. Qualquer outro endereço é recusado.</param>
public sealed record ImportIndexCompositionRequest(
    IReadOnlyList<string> Urls);
