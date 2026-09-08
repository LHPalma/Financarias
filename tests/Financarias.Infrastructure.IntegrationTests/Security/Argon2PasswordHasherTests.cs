using Financarias.Domain.Identity;
using Financarias.Infrastructure.Security;
using Isopoh.Cryptography.Argon2;

namespace Financarias.Infrastructure.IntegrationTests.Security;

public class Argon2PasswordHasherTests
{
    private const string Secret = "S3nha-Secreta!";

    private readonly Argon2PasswordHasher _hasher = new();

    [Fact(DisplayName = "O hash produzido declara argon2id e os parâmetros escolhidos")]
    public void Hash_AnnouncesArgon2idAndItsParameters()
    {
        // Act
        var hash = _hasher.Hash(Password.Create(Secret));

        // Assert: trava algoritmo e custo no teste — mudá-los em silêncio passa a quebrar aqui
        Assert.StartsWith("$argon2id$", hash.Value);
        Assert.Contains("m=19456,t=2,p=1", hash.Value);
    }

    [Fact(DisplayName = "A mesma senha gera hashes diferentes, e os dois verificam")]
    public void Hash_UsesARandomSaltEveryTime()
    {
        // Act
        var first = _hasher.Hash(Password.Create(Secret));
        var second = _hasher.Hash(Password.Create(Secret));

        // Assert: sem salt aleatório, senhas iguais teriam hashes iguais e um vazamento do
        // banco entregaria quem repetiu senha com quem
        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify(first, Secret));
        Assert.True(_hasher.Verify(second, Secret));
    }

    [Fact(DisplayName = "Senha errada não verifica")]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        // Arrange
        var hash = _hasher.Hash(Password.Create(Secret));

        // Act & Assert
        Assert.False(_hasher.Verify(hash, "S3nha-Errada!"));
    }

    [Fact(DisplayName = "Verify aceita senha que não passaria na política de hoje")]
    public void Verify_AcceptsPasswordThatWouldFailTodaysPolicy()
    {
        // Arrange: hash gravado quando a política era outra — "senhafraca" não tem maiúscula,
        // dígito nem especial, e Password.Create a rejeitaria
        var legacy = _hasher.Hash(Password.Create("Antiga-1!"));
        var older = PasswordHash.Create(Argon2.Hash(
            "senhafraca",
            timeCost: 1,
            memoryCost: 8192,
            parallelism: 1,
            type: Argon2Type.HybridAddressing,
            hashLength: 32));

        // Act & Assert: Verify recebe string, não Password, justamente para que endurecer a
        // política não tranque para fora quem cadastrou antes
        Assert.True(_hasher.Verify(older, "senhafraca"));
        Assert.True(_hasher.Verify(legacy, "Antiga-1!"));
        Assert.Throws<Financarias.Domain.Common.Exceptions.DomainValidationException>(
            () => Password.Create("senhafraca"));
    }
}
