namespace Financarias.Application.MarketData.SearchInterest.Import;

/// <summary>Resultado da importação do interesse de busca de um grupo de termos.</summary>
/// <param name="PointsRead">Pontos (termo e dia) lidos da fonte. Cada termo traz um por dia do intervalo.</param>
/// <param name="PointsCreated">Pontos novos gravados.</param>
/// <param name="PointsUpdated">Pontos que já existiam para o mesmo grupo, intervalo, termo e dia e tinham outro valor, atualizados. A fonte pode revisar a série.</param>
public sealed record SearchInterestImportResult(
    int PointsRead,
    int PointsCreated,
    int PointsUpdated);
