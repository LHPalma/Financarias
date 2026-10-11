using Financarias.Domain.MarketData.Indexes;

namespace Financarias.Application.MarketData.Indexes.DTOs.Requests;

/// <summary>
/// Ano de um índice a importar.
/// </summary>
/// <param name="Index">Índice cujo fechamento diário será importado.</param>
/// <param name="Year">Ano inteiro, a partir de 1968. Um ano ainda sem pregões importa zero linhas, sem erro.</param>
public sealed record ImportIndexQuotesRequest(
    MarketIndex Index,
    int Year);
