namespace Financarias.Domain.Analytics.Projections;

/// <summary>
///     Projeta o VNA da NTN-B para a data de liquidação pela metodologia ANBIMA/STN:
///     pró-rata em DIAS CORRIDOS entre os dias 15 (aniversário do IPCA), com o fator
///     pró-rata (<c>pr1</c>) truncado em 14 casas e o VNA projetado em 6.
/// </summary>
public static class VnaProjection
{
    /// <summary>
    /// Projeta o VNA para a data de liquidação via pró-rata do IPCA em DIAS CORRIDOS
    /// entre os dias 15 (aniversário do IPCA) — não usa feriados.
    /// </summary>
    /// <remarks>
    ///     Fórmula (fonte: STN, "Metodologia de Cálculo dos Títulos Públicos Federais", NTN-B):
    ///     <code>
    /// VNA_projetado = trunc6(vnaBase × (1 + π)^pr1)
    /// pr1 = trunc14(diasCorridos(liquidação − 15ºÂncora) / diasCorridos(15ºSeguinte − 15ºÂncora))
    /// </code>
    ///     O 15º âncora é o aniversário do IPCA: liquidação ≥ dia 15 → âncora no 15 do mês corrente
    ///     (e o seguinte no 15 do mês posterior); caso contrário, âncora no 15 do mês anterior.
    ///     Ex. (STN): liquidação 21/05/2008 → pr1 = (21/05 − 15/05) / (15/06 − 15/05) = 6/31.
    /// </remarks>
    /// <param name="vnaBase">VNA no dia 15 âncora (último VNA publicado).</param>
    /// <param name="projectedInflation">Projeção do IPCA do mês, em fração.</param>
    /// <param name="settlementDate">Data de liquidação (T+2).</param>
    /// <returns>O VNA projetado para a liquidação, truncado em 6 casas.</returns>
    public static decimal Project(decimal vnaBase, decimal projectedInflation, DateOnly settlementDate)
    {
        var anchor = AnchorFifteenth(settlementDate);
        var nextAnchor = anchor.AddMonths(1);

        var elapsedDays = settlementDate.DayNumber - anchor.DayNumber;
        var periodDays = nextAnchor.DayNumber - anchor.DayNumber;

        var proRata = Truncation.Truncate((decimal)elapsedDays / periodDays, 14);

        var factor = (decimal)Math.Pow((double)(1m + projectedInflation), (double)proRata);

        return Truncation.Truncate(vnaBase * factor, 6);
    }

    private static DateOnly AnchorFifteenth(DateOnly settlementDate)
    {
        var fifteenthThisMonth = new DateOnly(settlementDate.Year, settlementDate.Month, 15);
        return settlementDate.Day >= 15 ? fifteenthThisMonth : fifteenthThisMonth.AddMonths(-1);
    }
}