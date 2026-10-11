namespace Financarias.Application.MarketData.Inflation.Commands;

/// <summary>Resultado do recálculo do índice de inflação diário.</summary>
/// <param name="FirstDay">Primeiro dia com nível: o dia 15 do primeiro mês do IPCA, passado para o próximo dia útil se necessário.</param>
/// <param name="LastDay">Último dia com nível: o dia 15 seguinte ao último mês do IPCA, passado para o próximo dia útil se necessário. Datas depois dele não têm nível.</param>
/// <param name="Days">Quantos dias corridos o índice cobre, de `firstDay` a `lastDay`, ambos incluídos.</param>
/// <param name="LevelsCreated">Níveis de dias que ainda não existiam e foram gravados.</param>
/// <param name="LevelsUpdated">Níveis já gravados cujo valor mudou, porque o IPCA ou os feriados mudaram.</param>
/// <param name="LevelsRemoved">Níveis de dias que saíram do intervalo e foram removidos.</param>
public sealed record InflationLevelsRebuildResult(
    DateOnly FirstDay,
    DateOnly LastDay,
    int Days,
    int LevelsCreated,
    int LevelsUpdated,
    int LevelsRemoved);
