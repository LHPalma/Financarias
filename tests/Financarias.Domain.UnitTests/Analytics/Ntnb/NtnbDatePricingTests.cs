using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Ntnb;
using Financarias.Domain.Analytics.Projections;

namespace Financarias.Domain.UnitTests.Analytics.Ntnb;

public class NtnbDatePricingTests
{
    [Fact(DisplayName = "Compõe liquidação T+2 (pulando feriado), du em [liquidação, vencimento) e PU")]
    public void Calculate_ComposesSettlementBusinessDaysAndPrice()
    {
        // Arrange — 02/01/2024 (terça); feriado em 03/01 empurra o T+2
        var vnaBase = NominalValue.Create(1000m);
        var yield = AnnualYield.FromFraction(0.06m);
        const decimal inflation = 0.0046m;
        var tradeDate = new DateOnly(2024, 1, 2);
        var dueDate = new DateOnly(2024, 1, 15);
        IReadOnlySet<DateOnly> holidays = new HashSet<DateOnly> { new(2024, 1, 3) };

        // Act
        var result = NtnbDatePricing.Calculate(vnaBase, yield, inflation, tradeDate, dueDate, holidays);

        // Assert
        // T+2 de 02/01 com 03/01 feriado: 04/01 (+1), 05/01 (+2)
        Assert.Equal(new DateOnly(2024, 1, 5), result.SettlementDate);

        // du em [05/01, 15/01): 05, 08, 09, 10, 11, 12 (06-07 e 13-14 são fim de semana) = 6
        Assert.Equal(6, result.BusinessDaysToMaturity.Value);

        // Wiring: VNA projetado e PU vêm dos componentes corretos (não recalculados aqui)
        var expectedVna = VnaProjection.Project(vnaBase.Value, inflation, result.SettlementDate);
        Assert.Equal(expectedVna, result.ProjectedVna);
        Assert.Equal(NtnbPricing.UnitPrice(expectedVna, yield, result.BusinessDaysToMaturity), result.UnitPrice);
    }

    [Fact(DisplayName = "No vencimento (du = 0), o PU é o próprio VNA projetado")]
    public void Calculate_WhenSettlementEqualsMaturity_PriceEqualsProjectedVna()
    {
        // Arrange — 11/01 (quinta), sem feriados: T+2 = 15/01; vencimento 15/01 → du = 0
        var vnaBase = NominalValue.Create(1000m);
        var yield = AnnualYield.FromFraction(0.06m);
        var tradeDate = new DateOnly(2024, 1, 11);
        var dueDate = new DateOnly(2024, 1, 15);
        IReadOnlySet<DateOnly> holidays = new HashSet<DateOnly>();

        // Act
        var result = NtnbDatePricing.Calculate(vnaBase, yield, 0.0046m, tradeDate, dueDate, holidays);

        // Assert
        Assert.Equal(new DateOnly(2024, 1, 15), result.SettlementDate);
        Assert.Equal(0, result.BusinessDaysToMaturity.Value);
        Assert.Equal(result.ProjectedVna, result.UnitPrice); // du = 0 → cotação 100% → PU = VNA projetado
    }
}
