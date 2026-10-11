using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.CorporateEvents;

namespace Financarias.Domain.UnitTests.MarketData.CorporateEvents;

public class CorporateEventTests
{
    private static readonly DateOnly CumDate = new(2026, 8, 21);

    [Fact(DisplayName = "Create monta um provento em dinheiro com valor, lote e datas")]
    public void Create_CashDividend_SetsProperties()
    {
        // Act
        var cashEvent = CorporateEvent.Create(
            "PETR", "BRPETRACNOR9", "ON", CorporateEventKind.CashDividend, CumDate,
            new DateOnly(2026, 8, 6), new DateOnly(2026, 12, 21), 0.47156696m, 1, null, "DIVIDENDO");

        // Assert
        Assert.Equal("PETR", cashEvent.IssuerCode);
        Assert.Equal("BRPETRACNOR9", cashEvent.Isin);
        Assert.Equal("ON", cashEvent.ShareClass);
        Assert.Equal(CorporateEventKind.CashDividend, cashEvent.Kind);
        Assert.Equal(CumDate, cashEvent.CumDate);
        Assert.Equal(new DateOnly(2026, 8, 6), cashEvent.ApprovalDate);
        Assert.Equal(new DateOnly(2026, 12, 21), cashEvent.PaymentDate);
        Assert.Equal(0.47156696m, cashEvent.Amount);
        Assert.Equal(1, cashEvent.PerShares);
        Assert.Null(cashEvent.Factor);
        Assert.Equal("DIVIDENDO", cashEvent.Label);
    }

    [Fact(DisplayName = "AmountPerShare divide o valor pelo lote, como no provento de 1996 cotado por mil ações")]
    public void AmountPerShare_DividesByTheQuoteLot()
    {
        // Arrange
        var cashEvent = CorporateEvent.Create(
            "PETR", null, "PN", CorporateEventKind.CashDividend, new DateOnly(1996, 3, 21),
            null, null, 5.1471m, 1000, null, "DIVIDENDO");

        // Assert
        Assert.Equal(0.0051471m, cashEvent.AmountPerShare);
    }

    [Theory(DisplayName = "Dividendo, JCP e rendimento são eventos em dinheiro")]
    [InlineData(CorporateEventKind.CashDividend)]
    [InlineData(CorporateEventKind.InterestOnEquity)]
    [InlineData(CorporateEventKind.Yield)]
    public void IsCash_IsTrue_ForCashKinds(CorporateEventKind kind)
    {
        // Act & Assert
        Assert.True(CorporateEvent.IsCash(kind));
    }

    [Theory(DisplayName = "Desdobramento, grupamento e bonificação não são eventos em dinheiro")]
    [InlineData(CorporateEventKind.StockSplit)]
    [InlineData(CorporateEventKind.ReverseSplit)]
    [InlineData(CorporateEventKind.BonusShares)]
    public void IsCash_IsFalse_ForStockKinds(CorporateEventKind kind)
    {
        // Act & Assert
        Assert.False(CorporateEvent.IsCash(kind));
    }

    [Fact(DisplayName = "Create monta um desdobramento com o fator como publicado, sem valor em dinheiro")]
    public void Create_StockSplit_SetsFactor()
    {
        // Act
        var split = CorporateEvent.Create(
            "PETR", "BRPETRACNPR6", "PN", CorporateEventKind.StockSplit, new DateOnly(2008, 4, 25),
            new DateOnly(2008, 4, 25), null, null, null, 100m, "DESDOBRAMENTO");

        // Assert
        Assert.Equal(100m, split.Factor);
        Assert.Null(split.Amount);
        Assert.Null(split.AmountPerShare);
    }

    [Fact(DisplayName = "Create aceita valor zero em dinheiro (rendimento zerado publicado pela B3)")]
    public void Create_AcceptsZeroAmount()
    {
        // Act
        var cashEvent = CorporateEvent.Create(
            "PETR", null, "ON", CorporateEventKind.Yield, CumDate, null, null, 0m, 1, null, "RENDIMENTO");

        // Assert
        Assert.Equal(0m, cashEvent.Amount);
    }

    [Theory(DisplayName = "Create lança erro de domínio para combinação inválida de campos")]
    [InlineData("PETR", "ON", CorporateEventKind.CashDividend, null, 1, null)]      // sem valor
    [InlineData("PETR", "ON", CorporateEventKind.CashDividend, -0.1, 1, null)]       // valor negativo
    [InlineData("PETR", "ON", CorporateEventKind.CashDividend, 0.5, null, null)]     // sem lote
    [InlineData("PETR", "ON", CorporateEventKind.CashDividend, 0.5, 0, null)]        // lote zero
    [InlineData("PETR", "ON", CorporateEventKind.InterestOnEquity, 0.5, 1, 10.0)]    // dinheiro com fator
    [InlineData("PETR", "ON", CorporateEventKind.StockSplit, null, null, null)]      // sem fator
    [InlineData("PETR", "ON", CorporateEventKind.ReverseSplit, null, null, 0.0)]     // fator zero
    [InlineData("PETR", "ON", CorporateEventKind.BonusShares, 1.0, null, 33.3)]      // ações com valor
    [InlineData("PET", "ON", CorporateEventKind.CashDividend, 0.5, 1, null)]         // emissor curto
    [InlineData("PETRO", "ON", CorporateEventKind.CashDividend, 0.5, 1, null)]       // emissor longo
    [InlineData("pet4", "ON", CorporateEventKind.CashDividend, 0.5, 1, null)]        // minúsculas
    [InlineData("PETR", " ", CorporateEventKind.CashDividend, 0.5, 1, null)]         // espécie vazia
    public void Create_WithInvalidCombination_ThrowsDomainError(
        string issuerCode, string shareClass, CorporateEventKind kind, double? amount, int? perShares, double? factor)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            CorporateEvent.Create(
                issuerCode, null, shareClass, kind, CumDate, null, null, (decimal?)amount, perShares, (decimal?)factor, "X"));

        Assert.Equal("corporate.event.invalid", exception.Code);
    }

    [Theory(DisplayName = "IsValidIssuerCode aceita quatro letras ou dígitos em maiúsculas e recusa o resto")]
    [InlineData("PETR", true)]
    [InlineData("B3SA", true)]
    [InlineData("AAPL", true)]
    [InlineData("PET", false)]
    [InlineData("PETRO", false)]
    [InlineData("petr", false)]
    [InlineData("PE R", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidIssuerCode_AcceptsOnlyFourCapitalLettersOrDigits(string? issuerCode, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, CorporateEvent.IsValidIssuerCode(issuerCode));
    }

    [Fact(DisplayName = "GetError devolve nulo para um evento válido, sem lançar")]
    public void GetError_ReturnsNull_ForValidEvent()
    {
        // Act & Assert
        Assert.Null(CorporateEvent.GetError("PETR", "ON", CorporateEventKind.CashDividend, 0.5m, 1, null));
    }
}
