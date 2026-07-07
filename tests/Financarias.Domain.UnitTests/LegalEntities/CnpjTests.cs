using Financarias.Domain.LegalEntities;

namespace Financarias.Domain.UnitTests.LegalEntities;

public class CnpjTests
{
    [Theory(DisplayName = "Cria CNPJ normalizando para 14 dígitos sem máscara")]
    [InlineData("01492748000383", "01492748000383")]
    [InlineData("01.492.748/0003-83", "01492748000383")]
    [InlineData(" 01.492.748/0003-83 ", "01492748000383")]
    public void Create_WithValidInput_NormalizesValue(string input, string expected)
    {
        // Act
        var cnpj = Cnpj.Create(input);

        // Assert
        Assert.Equal(expected, cnpj.Value);
    }

    [Fact(DisplayName = "Expõe a forma com máscara em Formatted")]
    public void Formatted_WithValidCnpj_ReturnsMaskedValue()
    {
        // Arrange
        var cnpj = Cnpj.Create("01492748000383");

        // Act
        var formatted = cnpj.Formatted;

        // Assert
        Assert.Equal("01.492.748/0003-83", formatted);
    }

    [Theory(DisplayName = "Lança InvalidCnpjException para entrada inválida")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0149274800038")]      // 13 dígitos
    [InlineData("014927480003835")]    // 15 dígitos
    [InlineData("01.492.748/0003-8a")] // contém letra
    public void Create_WithInvalidInput_ThrowsInvalidCnpjException(string? input)
    {
        // Act & Assert
        Assert.Throws<InvalidCnpjException>(() => Cnpj.Create(input));
    }

    [Theory(DisplayName = "TryCreate retorna true e produz o CNPJ para entrada válida")]
    [InlineData("01492748000383")]
    [InlineData("01.492.748/0003-83")]
    public void TryCreate_WithValidInput_ReturnsTrueAndCnpj(string input)
    {
        // Act
        var ok = Cnpj.TryCreate(input, out var cnpj);

        // Assert
        Assert.True(ok);
        Assert.NotNull(cnpj);
        Assert.Equal("01492748000383", cnpj!.Value);
    }

    [Theory(DisplayName = "TryCreate retorna false e null para entrada inválida")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefghijklmn")]
    public void TryCreate_WithInvalidInput_ReturnsFalseAndNull(string? input)
    {
        // Act
        var ok = Cnpj.TryCreate(input, out var cnpj);

        // Assert
        Assert.False(ok);
        Assert.Null(cnpj);
    }

    [Fact(DisplayName = "Dois CNPJs com o mesmo valor são iguais (igualdade por valor)")]
    public void Equality_WithSameValue_AreEqual()
    {
        // Arrange
        var a = Cnpj.Create("01.492.748/0003-83");
        var b = Cnpj.Create("01492748000383");

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact(DisplayName = "ToString retorna o valor canônico")]
    public void ToString_ReturnsCanonicalValue()
    {
        // Act & Assert
        Assert.Equal("01492748000383", Cnpj.Create("01.492.748/0003-83").ToString());
    }
}
