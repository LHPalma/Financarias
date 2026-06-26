using Financarias.Domain.Addresses;

namespace Financarias.Domain.UnitTests.Addresses;

public class CepTests
{
    [Theory(DisplayName = "Cria CEP normalizando para 8 dígitos sem máscara")]
    [InlineData("01001000", "01001000")]
    [InlineData("01001-000", "01001000")]
    [InlineData(" 01001-000 ", "01001000")]
    public void Create_WithValidInput_NormalizesValue(string input, string expected)
    {
        // Act
        var cep = Cep.Create(input);

        // Assert
        Assert.Equal(expected, cep.Value);
    }

    [Fact(DisplayName = "Expõe a forma com máscara em Formatted")]
    public void Formatted_WithValidCep_ReturnsMaskedValue()
    {
        // Arrange
        var cep = Cep.Create("01001000");

        // Act
        var formatted = cep.Formatted;

        // Assert
        Assert.Equal("01001-000", formatted);
    }

    [Theory(DisplayName = "Lança InvalidCepException para entrada inválida")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0100100")]     // 7 dígitos
    [InlineData("010010000")]   // 9 dígitos
    [InlineData("01001-00a")]   // contém letra
    public void Create_WithInvalidInput_ThrowsInvalidCepException(string? input)
    {
        // Act & Assert
        Assert.Throws<InvalidCepException>(() => Cep.Create(input));
    }

    [Theory(DisplayName = "TryCreate retorna true e produz o CEP para entrada válida")]
    [InlineData("01001000")]
    [InlineData("01001-000")]
    public void TryCreate_WithValidInput_ReturnsTrueAndCep(string input)
    {
        // Act
        var ok = Cep.TryCreate(input, out var cep);

        // Assert
        Assert.True(ok);
        Assert.NotNull(cep);
        Assert.Equal("01001000", cep!.Value);
    }

    [Theory(DisplayName = "TryCreate retorna false e null para entrada inválida")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefgh")]
    public void TryCreate_WithInvalidInput_ReturnsFalseAndNull(string? input)
    {
        // Act
        var ok = Cep.TryCreate(input, out var cep);

        // Assert
        Assert.False(ok);
        Assert.Null(cep);
    }

    [Fact(DisplayName = "Dois CEPs com o mesmo valor são iguais (igualdade por valor)")]
    public void Equality_WithSameValue_AreEqual()
    {
        // Arrange
        var a = Cep.Create("01001-000");
        var b = Cep.Create("01001000");

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact(DisplayName = "ToString retorna o valor canônico")]
    public void ToString_ReturnsCanonicalValue()
    {
        // Act & Assert
        Assert.Equal("01001000", Cep.Create("01001-000").ToString());
    }
}
