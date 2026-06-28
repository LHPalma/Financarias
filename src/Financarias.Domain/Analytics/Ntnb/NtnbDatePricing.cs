using Financarias.Domain.Analytics.Projections;
using Financarias.Domain.Calendar;

namespace Financarias.Domain.Analytics.Ntnb;

/// <summary>
///     Precifica a NTN-B Principal por datas (metodologia ANBIMA/STN): liquidação T+2,
///     VNA projetado para a liquidação e du em [liquidação, vencimento) — depois o PU.
/// </summary>
public static class NtnbDatePricing
{
    private const int SettlementBusinessdays = 2; // T+2

    /// <summary>
    ///     Calcula o PU da NTN-B Principal por datas, orquestrando as etapas da metodologia ANBIMA/STN.
    /// </summary>
    /// <remarks>
    ///     1) liquidação = tradeDate + 2 dias úteis (T+2);
    ///     2) VNA projetado para a liquidação — ver <see cref="VnaProjection" />;
    ///     3) du = dias úteis em [liquidação, vencimento) (liquidação inclusive, vencimento exclusive);
    ///     4) PU a partir do VNA projetado, da taxa e do du — ver <see cref="NtnbPricing" />.
    /// </remarks>
    /// <param name="vnaBase">VNA no dia 15 âncora (último VNA publicado).</param>
    /// <param name="yield">Taxa interna de retorno anual, em fração.</param>
    /// <param name="projectedInflation">Projeção do IPCA do mês de referência, em fração.</param>
    /// <param name="tradeDate">Data de negociação (data de cálculo).</param>
    /// <param name="dueDate">Data de vencimento do título.</param>
    /// <param name="holidays">Feriados do país no intervalo, para a contagem de dias úteis.</param>
    /// <returns>Liquidação, VNA projetado, dias úteis até o vencimento e o PU.</returns>
    public static NtnbPrice Calculate(
        NominalValue vnaBase,
        AnnualYield yield,
        decimal projectedInflation,
        DateOnly tradeDate,
        DateOnly dueDate,
        IReadOnlySet<DateOnly> holidays)
    {
        var settlement = BusinessDayCalendar.AddBusinessDays(tradeDate, SettlementBusinessdays, holidays);

        var projectedVna = VnaProjection.Project(vnaBase.Value, projectedInflation, settlement);

        var businessDays = BusinessDayCalendar.Count(settlement.AddDays(-1), dueDate.AddDays(-1), holidays);

        var unitPrice = NtnbPricing.UnitPrice(projectedVna, yield, businessDays);

        return new NtnbPrice(
            settlement,
            projectedVna,
            businessDays,
            unitPrice
        );
    }
}