using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Issuers;

namespace Financarias.Domain.UnitTests.MarketData.Issuers;

public class IssuerTests
{
    private static Issuer Petrobras() => Issuer.Create(
        "PETR",
        "9512",
        "PETROLEO BRASILEIRO S.A. PETROBRAS",
        "PETROBRAS",
        Cnpj.Create("33000167000101"),
        "Petróleo. Gás e Biocombustíveis",
        "Petróleo. Gás e Biocombustíveis",
        "Exploração. Refino e Distribuição",
        "BOVESPA NIVEL 2",
        "Pesquisa. lavra. refinação.",
        "www.petrobras.com.br");

    [Fact(DisplayName = "Create monta o emissor com a classificação setorial")]
    public void Create_SetsTheFields()
    {
        // Act
        var issuer = Petrobras();

        // Assert
        Assert.Equal("PETR", issuer.IssuerCode);
        Assert.Equal("9512", issuer.CvmCode);
        Assert.Equal("PETROBRAS", issuer.TradingName);
        Assert.Equal("33000167000101", issuer.Cnpj?.Value);
        Assert.Equal("Petróleo. Gás e Biocombustíveis", issuer.Sector);
        Assert.Equal("Exploração. Refino e Distribuição", issuer.Segment);
    }

    [Fact(DisplayName = "Create aceita emissor sem CNPJ nem classificação")]
    public void Create_AcceptsMissingOptionalFields()
    {
        // Act
        var issuer = Issuer.Create("UQMU", "900049", "1461 INVESTIMENTOS S.A", "1461 INVEST", null, null, null, null, null, null, null);

        // Assert
        Assert.Null(issuer.Cnpj);
        Assert.Null(issuer.Sector);
    }

    [Theory(DisplayName = "Create lança erro de domínio quando o código do emissor, o código CVM ou um dos nomes é inválido")]
    [InlineData("petr", "9512", "Empresa", "Nome")]
    [InlineData("PETR4", "9512", "Empresa", "Nome")]
    [InlineData("PETR", " ", "Empresa", "Nome")]
    [InlineData("PETR", "9512", "", "Nome")]
    [InlineData("PETR", "9512", "Empresa", "  ")]
    public void Create_WithInvalidField_ThrowsDomainError(string issuerCode, string cvmCode, string companyName, string tradingName)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            Issuer.Create(issuerCode, cvmCode, companyName, tradingName, null, null, null, null, null, null, null));

        Assert.Equal("issuer.company.invalid", exception.Code);
    }

    [Fact(DisplayName = "Update devolve falso e não muda nada quando os dados são os mesmos")]
    public void Update_WithSameData_ReturnsFalse()
    {
        // Arrange
        var issuer = Petrobras();

        // Act
        var changed = issuer.Update(
            "9512", "PETROLEO BRASILEIRO S.A. PETROBRAS", "PETROBRAS", Cnpj.Create("33.000.167/0001-01"),
            "Petróleo. Gás e Biocombustíveis", "Petróleo. Gás e Biocombustíveis", "Exploração. Refino e Distribuição",
            "BOVESPA NIVEL 2", "Pesquisa. lavra. refinação.", "www.petrobras.com.br");

        // Assert
        Assert.False(changed);
    }

    [Fact(DisplayName = "Update troca os campos que mudaram e devolve verdadeiro")]
    public void Update_WithNewData_ReplacesTheFields()
    {
        // Arrange
        var issuer = Petrobras();

        // Act
        var changed = issuer.Update(
            "9512", "PETROLEO BRASILEIRO S.A. PETROBRAS", "PETROBRAS", Cnpj.Create("33000167000101"),
            "Petróleo, Gás e Biocombustíveis", null, null, "BOVESPA NIVEL 1", null, null);

        // Assert
        Assert.True(changed);
        Assert.Equal("Petróleo, Gás e Biocombustíveis", issuer.Sector);
        Assert.Null(issuer.Subsector);
        Assert.Equal("BOVESPA NIVEL 1", issuer.ListingMarket);
    }

    [Fact(DisplayName = "Update com nome vazio lança erro de domínio e preserva os dados")]
    public void Update_WithInvalidName_ThrowsAndKeepsTheData()
    {
        // Arrange
        var issuer = Petrobras();

        // Act
        var exception = Assert.Throws<DomainValidationException>(() =>
            issuer.Update("9512", "", "PETROBRAS", null, null, null, null, null, null, null));

        // Assert
        Assert.Equal("issuer.company.invalid", exception.Code);
        Assert.Equal("PETROLEO BRASILEIRO S.A. PETROBRAS", issuer.CompanyName);
    }
}
