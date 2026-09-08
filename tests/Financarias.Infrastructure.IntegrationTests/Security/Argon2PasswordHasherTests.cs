using Financarias.Domain.Identity;
using Financarias.Infrastructure.Security;
using Isopoh.Cryptography.Argon2;

namespace Financarias.Infrastructure.IntegrationTests.Security;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact(DisplayName = "O hash produzido declara argon2id e os parâmetros escolhidos")]
    public void Hash_AnnouncesArgon2idAndItsParameters()
    {
        // Act
        var hash = _hasher.Hash("senha-secreta");

        // Assert: trava algoritmo e custo no teste — mudá-los em silêncio passa a quebrar aqui
        Assert.StartsWith("$argon2id$", hash.Value);
        Assert.Contains("m=19456,t=2,p=1", hash.Value);
    }

    [Fact(DisplayName = "A mesma senha gera hashes diferentes, e os dois verificam")]
    public void Hash_UsesARandomSaltEveryTime()
    {
        // Act
        var first = _hasher.Hash("senha-secreta");
        var second = _hasher.Hash("senha-secreta");

        // Assert: sem salt aleatório, senhas iguais teriam hashes iguais e um vazamento
        // do banco entregaria quem repetiu senha com quem
        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify(first, "senha-secreta"));
        Assert.True(_hasher.Verify(second, "senha-secreta"));
    }

    [Fact(DisplayName = "Senha errada não verifica")]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        // Arrange
        var hash = _hasher.Hash("senha-secreta");

        // Act & Assert
        Assert.False(_hasher.Verify(hash, "senha-errada"));
    }

    [Fact(DisplayName = "Hash gravado com outros parâmetros continua verificando")]
    public void Verify_ReturnsTrue_ForHashProducedWithOtherParameters()
    {
        // Arrange: os parâmetros vivem na própria string PHC, então subir o custo do adapter
        // não invalida o que já está no banco
        var older = PasswordHash.Create(Argon2.Hash(
            "senha-secreta",
            timeCost: 1,
            memoryCost: 8192,
            parallelism: 1,
            type: Argon2Type.HybridAddressing,
            hashLength: 32));

        // Act & Assert
        Assert.True(_hasher.Verify(older, "senha-secreta"));
    }
}
