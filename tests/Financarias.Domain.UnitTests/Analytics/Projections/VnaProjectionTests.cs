using Financarias.Domain.Analytics.Projections;

namespace Financarias.Domain.UnitTests.Analytics.Projections;

public class VnaProjectionTests
{
    [Fact(DisplayName = "Usa pr1 = 6/31 no exemplo do PDF (liquidação 21/05/2008)")]
    public void Project_OnPdfExample_UsesProRataOfSixOverThirtyOne()
    {
        // Arrange — liquidação ≥ dia 15: âncora 15/05, seguinte 15/06; 6 dias corridos sobre 31.
        const decimal vnaBase = 1000m;
        const decimal inflation = 0.0046m;
        var settlement = new DateOnly(2008, 5, 21);

        // Act
        var projected = VnaProjection.Project(vnaBase, inflation, settlement);

        // Assert — recupera o expoente: projetado = vnaBase × (1 + ipca)^pr1
        var impliedProRata = Math.Log((double)(projected / vnaBase)) / Math.Log(1 + (double)inflation);
        Assert.Equal(6.0 / 31.0, impliedProRata, precision: 4);
    }

    [Fact(DisplayName = "Antes do dia 15, ancora no 15 do mês anterior (pr1 = 25/30)")]
    public void Project_BeforeTheFifteenth_AnchorsToPreviousMonth()
    {
        // Arrange — liquidação 10/05 < dia 15: âncora 15/04, seguinte 15/05; 25 dias corridos sobre 30.
        const decimal vnaBase = 1000m;
        const decimal inflation = 0.0046m;
        var settlement = new DateOnly(2008, 5, 10);

        // Act
        var projected = VnaProjection.Project(vnaBase, inflation, settlement);

        // Assert
        var impliedProRata = Math.Log((double)(projected / vnaBase)) / Math.Log(1 + (double)inflation);
        Assert.Equal(25.0 / 30.0, impliedProRata, precision: 4);
    }

    [Fact(DisplayName = "No próprio dia 15 âncora, não projeta (fator 1) — só trunca o VNA base")]
    public void Project_OnAnchorFifteenth_ReturnsTruncatedBaseVna()
    {
        // Arrange — elapsed = 0 → pr1 = 0 → (1 + ipca)^0 = 1
        const decimal vnaBase = 1000m;
        var settlement = new DateOnly(2008, 5, 15);

        // Act
        var projected = VnaProjection.Project(vnaBase, 0.05m, settlement);

        // Assert
        Assert.Equal(1000m, projected);
    }

    [Fact(DisplayName = "Com inflação zero, devolve o VNA base truncado em 6 casas")]
    public void Project_WithZeroInflation_TruncatesBaseToSixDecimals()
    {
        // Act
        var projected = VnaProjection.Project(1234.5678901m, 0m, new DateOnly(2008, 5, 21));

        // Assert
        Assert.Equal(1234.56789m, projected);
    }
}
