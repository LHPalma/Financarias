using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Identity;
using Financarias.Infrastructure.Security;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Options;

namespace Financarias.Infrastructure.IntegrationTests.Security;

public class Argon2PasswordHasherTests
{
    private const string Secret = "S3nha-Secreta!";
    private const string PepperV1 = "pepper-versao-1-com-pelo-menos-32-caracteres";
    private const string PepperV2 = "pepper-versao-2-com-pelo-menos-32-caracteres";

    [Fact(DisplayName = "O hash produzido declara argon2id e os parâmetros escolhidos")]
    public void Hash_AnnouncesArgon2idAndItsParameters()
    {
        // Act
        var hash = Hasher(current: 1, (1, PepperV1)).Hash(Password.Create(Secret));

        // Assert: trava algoritmo e custo no teste — mudá-los em silêncio passa a quebrar aqui
        Assert.StartsWith("$argon2id$", hash.Value);
        Assert.Contains("m=19456,t=2,p=1", hash.Value);
    }

    [Fact(DisplayName = "O hash grava a versão corrente do pepper")]
    public void Hash_RecordsTheCurrentPepperVersion()
    {
        // Act
        var hash = Hasher(current: 2, (1, PepperV1), (2, PepperV2)).Hash(Password.Create(Secret));

        // Assert
        Assert.Equal(2, hash.PepperVersion);
    }

    [Fact(DisplayName = "O pepper não aparece na string PHC")]
    public void Hash_DoesNotLeakThePepper()
    {
        // Act
        var hash = Hasher(current: 1, (1, PepperV1)).Hash(Password.Create(Secret));

        // Assert: se o pepper fosse gravado junto, um dump do banco o entregaria
        Assert.DoesNotContain(PepperV1, hash.Value);
    }

    [Fact(DisplayName = "A mesma senha gera hashes diferentes, e os dois verificam")]
    public void Hash_UsesARandomSaltEveryTime()
    {
        // Arrange
        var hasher = Hasher(current: 1, (1, PepperV1));

        // Act
        var first = hasher.Hash(Password.Create(Secret));
        var second = hasher.Hash(Password.Create(Secret));

        // Assert
        Assert.NotEqual(first, second);
        Assert.True(hasher.Verify(first, Secret));
        Assert.True(hasher.Verify(second, Secret));
    }

    [Fact(DisplayName = "Senha errada não verifica")]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        // Arrange
        var hasher = Hasher(current: 1, (1, PepperV1));
        var hash = hasher.Hash(Password.Create(Secret));

        // Act & Assert
        Assert.False(hasher.Verify(hash, "S3nha-Errada!"));
    }

    [Fact(DisplayName = "Com outro pepper na mesma versão, a senha certa não verifica")]
    public void Verify_ReturnsFalse_WhenThePepperDiffers()
    {
        // Arrange: é o cenário do dump do banco usado com um pepper chutado
        var hash = Hasher(current: 1, (1, PepperV1)).Hash(Password.Create(Secret));
        var otherPepper = Hasher(current: 1, (1, PepperV2));

        // Act & Assert
        Assert.False(otherPepper.Verify(hash, Secret));
    }

    [Fact(DisplayName = "Hash derivado sem pepper não verifica num sistema com pepper")]
    public void Verify_ReturnsFalse_ForHashDerivedWithoutPepper()
    {
        // Arrange
        var unpeppered = PasswordHash.Create(
            Argon2.Hash(Secret, timeCost: 2, memoryCost: 19456, parallelism: 1,
                type: Argon2Type.HybridAddressing, hashLength: 32),
            1);

        // Act & Assert
        Assert.False(Hasher(current: 1, (1, PepperV1)).Verify(unpeppered, Secret));
    }

    [Fact(DisplayName = "Rotação: hash antigo continua verificando e os novos saem na versão nova")]
    public void Verify_KeepsOldHashesWorking_AfterRotation()
    {
        // Arrange
        var beforeRotation = Hasher(current: 1, (1, PepperV1));
        var oldHash = beforeRotation.Hash(Password.Create(Secret));

        var afterRotation = Hasher(current: 2, (1, PepperV1), (2, PepperV2));

        // Act
        var newHash = afterRotation.Hash(Password.Create(Secret));

        // Assert: é exatamente o que era impossível antes de versionar o pepper
        Assert.True(afterRotation.Verify(oldHash, Secret));
        Assert.True(afterRotation.Verify(newHash, Secret));
        Assert.Equal(1, oldHash.PepperVersion);
        Assert.Equal(2, newHash.PepperVersion);
    }

    [Fact(DisplayName = "Versão de pepper não configurada lança em vez de fingir senha errada")]
    public void Verify_Throws_WhenThePepperVersionIsNotConfigured()
    {
        // Arrange: pepper v1 aposentado cedo demais
        var hash = Hasher(current: 1, (1, PepperV1)).Hash(Password.Create(Secret));
        var retired = Hasher(current: 2, (2, PepperV2));

        // Act & Assert: devolver false faria um erro de configuração parecer "senha incorreta"
        var exception = Assert.Throws<InvalidOperationException>(() => retired.Verify(hash, Secret));

        Assert.Contains("Pepper version 1", exception.Message);
    }

    [Fact(DisplayName = "Verify aceita senha que não passaria na política de hoje")]
    public void Verify_AcceptsPasswordThatWouldFailTodaysPolicy()
    {
        // Arrange: hash gravado quando a política era outra, e com parâmetros de custo antigos
        var older = PasswordHash.Create(
            Argon2.Hash("senhafraca", PepperV1, timeCost: 1, memoryCost: 8192, parallelism: 1,
                type: Argon2Type.HybridAddressing, hashLength: 32),
            1);

        // Act & Assert: Verify recebe string, não Password, para que endurecer a política
        // não tranque para fora quem cadastrou antes
        Assert.True(Hasher(current: 1, (1, PepperV1)).Verify(older, "senhafraca"));
        Assert.Throws<DomainValidationException>(() => Password.Create("senhafraca"));
    }

    [Theory(DisplayName = "Com hash nulo, Verify devolve false para qualquer senha")]
    [InlineData("S3nha-Forte!")]
    [InlineData("")]
    [InlineData("Descartavel-games-e-jogos-7!")]
    public void Verify_ReturnsFalse_ForNullHash(string password)
    {
        // Arrange
        var hasher = Hasher(current: 1, (1, PepperV1));

        // Act & Assert: inclui a senha do próprio hash descartável — ele existe para gastar tempo,
        // nunca para autenticar ninguém
        Assert.False(hasher.Verify(null, password));
    }

    [Fact(DisplayName = "Verify com hash nulo funciona chamada várias vezes seguidas")]
    public void Verify_ReturnsFalse_OnRepeatedCallsWithNullHash()
    {
        // Arrange
        var hasher = Hasher(current: 1, (1, PepperV1));

        // Act
        var results = Enumerable.Range(0, 3).Select(_ => hasher.Verify(null, Secret)).ToList();

        // Assert: a segunda chamada em diante reaproveita o hash descartável já derivado
        Assert.All(results, Assert.False);
    }

    [Fact(DisplayName = "Hash nulo não impede a verificação normal do mesmo hasher")]
    public void Verify_StillWorks_AfterANullHashCall()
    {
        // Arrange
        var hasher = Hasher(current: 1, (1, PepperV1));
        var hash = hasher.Hash(Password.Create(Secret));

        // Act
        hasher.Verify(null, "qualquer");

        // Assert: o descartável é estado do hasher, e não pode contaminar quem tem hash de verdade
        Assert.True(hasher.Verify(hash, Secret));
        Assert.False(hasher.Verify(hash, "S3nha-Errada!"));
    }

    private static Argon2PasswordHasher Hasher(int current, params (int Version, string Pepper)[] peppers) =>
        new(Options.Create(new PasswordHashingOptions
        {
            CurrentPepperVersion = current,
            Peppers = peppers.ToDictionary(entry => entry.Version, entry => entry.Pepper)
        }));
}
